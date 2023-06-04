
var Helpers   = require('../../../../Helpers/Helpers');
var base_card = require('../../../../../data/card');

var Poker = function(poker, singID, game){
	this.poker  = poker;  // quản lý các phòng
	this.singID = singID; // ID phòng
	this.game   = game;   // game (100/1000/5000/10000/...)
	poker.addRoom(this);
	this.online   = 0;    // số người trong phòng
	this.card     = [];   // bộ bài
	this.mainCard = [];   // bài trên bàn

	// ghế ngồi có sẵn 
	this.player = {
		1: {id:1,
			data:null},
		2: {id:2,
			data:null},
		3: {id:3,
			data:null},
		4: {id:4,
			data:null},
		5: {id:5,
			data:null},
		6: {id:6,
			data:null},
	};

	this.playerInGame = []; // đang chơi

	this.isPlay       = false; // phòng đang chơi
	this.timeOut      = null;  // thời gian

	this.i_first      = 0;     // người chơi đầu tiên
	this.i_last       = 0;     // người chơi sau cùng

	this.game_player  = null;  // người chơi hiện tại
	this.game_to      = false; // Game đang trong quá trình tố
	this.game_bet     = 0;     // cược hiện tại của game

	this.game_time     = 0;    // mini time

	this.game_start    = false; // game đã bắt đầu

	this.timeStartGame = 5;   // thời gian bắt đầu game
	this.time_start    = 0;    // thời gian bắt đầu game
	this.timePlayer    = 15;   // thời gian người chơi lựa chọn

	this.regTimeStart  = null; // Đăng ký thời gian bắt đầu
};

Poker.prototype.sendTo = function(client, data){
	client.red(data);
}

Poker.prototype.sendToAll = function(data, player = null){
	Object.values(this.player).forEach(function(ghe){
		if (!!ghe.data && ghe.data !== player) {
			!!ghe.data.client && ghe.data.client.red(data);
		}
	});
}

Poker.prototype.inroom = function(player){
	this.online++;

	if (this.online > 5) {
		this.poker.removeRoom(this.game, this.singID);
	}else{
		this.poker.addRoom(this);
	}

	player.room = this;
	let trongPhong = Object.values(this.player); // danh sách ghế
	let gheTrong = trongPhong.filter(function(t){return t.data == null}); // lấy các ghế trống

	// lấy ngẫu nhiên 1 ghế trống và ngồi
	let rand = (Math.random()*gheTrong.length)>>0;
	gheTrong = gheTrong[rand];

	this.player[gheTrong.id].data = player; // ngồi
	player.map = gheTrong.id;               // vị trí ngồi
	let card = [];
	this.sendToAll({ingame:{ghe:player.map, data:{name:player.name, avatar:player.avatar, balans:player.balans}}}, player);
	let result = trongPhong.map(function(ghe){
		if (!!ghe.data) {
			if (ghe.data.isPlay === true) {
				card = card.concat({ghe:ghe.id, card:{}});
				return {ghe:ghe.id, data:{name:ghe.data.name, avatar:ghe.data.avatar, balans:ghe.data.balans, bet:ghe.data.bet}};
			}
			return {ghe:ghe.id, data:{name:ghe.data.name, avatar:ghe.data.avatar, balans:ghe.data.balans}};
		}else{
			return {ghe:ghe.id, data:null};
		}
	}.bind(this));
	let client = {infoGhe:result, infoRoom:{game:player.game, isPlay:this.isPlay, time_start:this.time_start, card:card}, meMap:player.map};
	if (this.isPlay === true) {
		client.game = {card:this.mainCard};
	}
	this.sendTo(player.client, client);
	this.online > 1 && this.checkGame(5000);
}

Poker.prototype.outroom = function(player){
	this.online--;
	this.player[player.map].data = null;
	this.onHuy(player);

	if (this.online < 1) {
		this.destroy();
	}else{
		this.sendToAll({outgame:player.map});
		if (this.online == 1) {
			this.resetData();
		}
	}
}

Poker.prototype.checkGame = function(tru = 0){
	if (!this.isPlay && !this.timeOut) {
		this.isPlay  = true;
		this.timeOut = setTimeout(function(){
			clearTimeout(this.timeOut);
			this.timeOut == null;
			this.time_start = this.timeStartGame;
			let nguoichoi = Object.values(this.player).filter(function(t){return t.data !== null});
			nguoichoi.forEach(function(player){
				player.data.newGame();
			}.bind(this));
			this.sendToAll({infoRoom:{time_start:this.timeStartGame, isPlay:true}});

			this.regTimeStart = setInterval(function(){
				if (this.time_start < 0) {
					clearInterval(this.regTimeStart);
					// ghế có người ngồi
					nguoichoi = Object.values(this.player).filter(function(t){return t.data !== null});
					if (nguoichoi.length < 2) {
						this.isPlay = false;
						return void 0;
					}
					this.playerInGame = [];
					nguoichoi.forEach(function(player, index){
						player.data.isPlay = true;
						this.playerInGame[index] = {id:player.data.map, data:player.data};
					}.bind(this));
					// Ngẫu nhiên người chơi đầu tiên.
					this.i_first = (Math.random()*this.playerInGame.length)>>0;

					let newGhe = [];
					if (this.i_first != 0) {
						newGhe = [...this.playerInGame.slice(this.i_first), ...this.playerInGame.slice(0, this.i_first)];
					}else{
						newGhe = this.playerInGame;
					}
					let tru100_1 = newGhe[newGhe.length-1];
					let tru100_2 = newGhe[newGhe.length-2];
					let tru50_1  = newGhe[newGhe.length-3];
					let tru50_2  = newGhe[newGhe.length-4];
					if (tru100_1) {
						tru100_1.data.bet = this.game*2;
						tru100_1.data.balans -= this.game*2;
						tru100_1.data.isTheo = true;
					}
					if (tru100_2) {
						tru100_2.data.bet = this.game*2;
						tru100_2.data.balans -= this.game*2;
						tru100_2.data.isTheo = true;
					}
					if (tru50_1) {
						tru50_1.data.bet = this.game;
						tru50_1.data.balans -= this.game;
					}
					if (tru50_2) {
						tru50_2.data.bet = this.game;
						tru50_2.data.balans -= this.game;
					}
					newGhe = null;
					this.game_bet = this.game*2;
					this.Round1();
				}
				this.time_start--;
			}.bind(this), 1000);
		}.bind(this), 8000-tru);
	}
}

// Vòng 1: Chia 2 lá đầu
Poker.prototype.Round1 = function(){
	this.card = [...base_card.card]; // bộ bài mới

	this.card = Helpers.shuffle(this.card); // tráo bài lần 1
	this.card = Helpers.shuffle(this.card); // tráo bài lần 2
	this.card = Helpers.shuffle(this.card); // tráo bài lần 3
	// chia bài
	let chia = [];
	let first = this.playerInGame.map(function(player, index){
		player.data.card = this.card.splice(0, 2);
		chia[index] = {id:player.id};
		return {id:player.id, bet:player.data.bet};
	}.bind(this));
	this.playerInGame.forEach(function(player){
		chia.forEach(function(dataChia){
			if (dataChia.id == player.id) {
				dataChia.data = player.data.card;
			}else{
				delete dataChia.data;
			}
		});
		this.sendTo(player.data.client, {game:{chia_bai:chia}, infoRoom:{bet:this.game_bet, first:first}})
	}.bind(this));

	first = null;

	clearTimeout(this.timeOut);
	this.timeOut = setTimeout(function(){
		clearTimeout(this.timeOut);
		this.nextPlayer(true);
	}.bind(this), 600);
}

// Sang vòng mới
Poker.prototype.nextRound = function(){
	this.game_to = false; // đã tố song
	let round = this.mainCard.length;
	if (round < 5) {
		if (round === 0) {
			// Mở 3 lá nên bàn
			this.mainCard = this.mainCard.concat(this.card.splice(0, 3));
			this.sendToAll({game:{card:this.mainCard}});
		}else{
			// mở thêm 1 lá
			let card = this.card.splice(0, 1);
			this.mainCard = this.mainCard.concat(card);
			this.sendToAll({game:{card:card}});
		}
		clearTimeout(this.timeOut);
		this.timeOut = setTimeout(function(){
			clearTimeout(this.timeOut);
			this.nextPlayer(true);
		}.bind(this), 300);
	}else{
		// Kết Thúc game - đã đủ 5 lá và tính điểm
		this.win();
	}
}

// tới lượt người chơi tiếp theo
Poker.prototype.nextPlayer = function(new_round = false){
	clearTimeout(this.timeOut);
	this.game_time = new Date().getTime();
	if(new_round === true){
		this.i_last = this.i_first;
		this.game_player = this.playerInGame[this.i_first].data;

		if (this.game_player.isOut === true || this.game_player.isHuy === true) {
			this.nextPlayer();
			return void 0;
		}

		let resultG = {ghe:this.game_player.map, progress:15};
		this.sendToAll({game:{turn:resultG}}, this.game_player);

		resultG = {ghe:this.game_player.map, progress:15, select:this.btnSelect(this.game_player)};
		this.sendTo(this.game_player.client, {game:{turn:resultG}});
		this.timeOut = setTimeout(function(){
			clearTimeout(this.timeOut);
			this.hetgio();
		}.bind(this), this.timePlayer*1000);
	}else{
		this.i_last++;
		if(this.i_last >= this.playerInGame.length){
			this.i_last = 0;
		}
		if (this.i_last === this.i_first) {
			// kết thúc vòng chơi
			this.sendToAll({game:{offSelect:true}});
			this.nextRound();
			return void 0;
		}

		this.game_player = this.playerInGame[this.i_last].data;
		if (this.game_player.isOut === true || this.game_player.isHuy === true) {
			this.nextPlayer();
			return void 0;
		}

		let resultG = {ghe:this.game_player.map, progress:15};
		this.sendToAll({game:{turn:resultG}}, this.game_player);

		resultG = {ghe:this.game_player.map, progress:15, select:this.btnSelect(this.game_player)};
		this.sendTo(this.game_player.client, {game:{turn:resultG}});

		this.timeOut = setTimeout(function(){
			clearTimeout(this.timeOut);
			this.hetgio();
		}.bind(this), this.timePlayer*1000);
	}
}

// Hết thời gian chưa chọn
Poker.prototype.hetgio = function(player){
	if (this.game_player) {
		if (this.game_player.isAll || (!this.game_to && this.game_player.isTheo)) {
			this.game_player.onXem();
		}else{
			this.game_player.onHuy();
		}
	}
}

Poker.prototype.btnSelect = function(player){
	let select = {xem:true, theo:true, to:true, all:true};
	if (player.isAll === true) {
		return {xem:true, theo:false, to:false, all:false};
	}
	if (this.game_to === true) {
		select.xem = false;
	}else{
		select.theo = false;
	}
	let bet = this.game_bet-player.bet;
	if (bet > player.balans) {
		select.theo = false;
		select.to   = false;
	}else if (bet == player.balans) {
		select.to = false;
	}
	return select;
}

// Theo
Poker.prototype.onTheo = function(player, xem){
	if (this.game_player === player) {
		let info = {};
		if (player.isAll) {
			this.sendToAll({game:{player:{ghe:player.map, data:{balans:player.balans, bet:player.bet}, info:{xem:0}}}});
			this.nextPlayer();
		}else{
			let bet = this.game_bet-player.bet;
			if (bet <= player.balans) {
				player.isTheo  = true;
				player.balans -= bet;
				player.bet    += bet;
				if (player.balans < 1) {
					player.isAll = true;
				}
				if (player.isAll) {
					info.all = bet;
				}else{
					if (xem) {
						info.xem  = bet;
					}else{
						info.theo = bet;
					}
				}
			}
			this.sendToAll({game:{player:{ghe:player.map, data:{balans:player.balans, bet:player.bet}, info:info}}});
			this.nextPlayer();
		}
	}
}
// Tố
Poker.prototype.onTo = function(player, to){
	if (this.game_player === player) {
		to = to>>0;
		let debit = this.game_bet-player.bet;   // số tiền đang thiếu
		let updateBalans = player.balans-debit; // số tiền còn lại khi trả đủ để thược hiện tố
		if (to <= updateBalans) {
			this.resetTheo();
			this.game_to  = true;
			player.balans = player.balans-(debit+to);
			player.bet    = player.bet+debit+to;
			this.game_bet += to;
			if (player.balans < 1) {
				player.isAll = true;
			}
			this.i_first = this.playerInGame.findIndex(function(obj){
				return (obj.id == player.map);
			}.bind(this));
			this.sendToAll({game:{player:{ghe:player.map, data:{balans:player.balans, bet:player.bet}, info:{to:to}}}, infoRoom:{bet:this.game_bet}});
			this.nextPlayer();
		}
	}
}

// Tất tay
Poker.prototype.onAll = function(player){
	if (this.game_player === player && player.isAll == false) {
		let backup = player.balans;
		let debit = this.game_bet-player.bet;   // số tiền đang thiếu
		let updateBalans = player.balans-debit; // số tiền còn lại khi trả đủ để thược hiện tố
		player.bet   += player.balans;
		player.isAll  = true;
		player.balans = 0;
		if (updateBalans > 0) {
			this.resetTheo();
			this.game_to   = true;
			this.game_bet += updateBalans;
			this.i_first = this.playerInGame.findIndex(function(obj){
				return (obj.id == player.map);
			}.bind(this));
		}
		this.sendToAll({game:{player:{ghe:player.map, data:{balans:player.balans, bet:player.bet}, info:{all:backup}}}, infoRoom:{bet:this.game_bet}});
		this.nextPlayer();
	}
}

Poker.prototype.resetTheo = function(){
	this.playerInGame.forEach(function(player){
		player.isTheo = false;
	});
}
Poker.prototype.onHuy = function(player){
	if (this.isPlay === true) {
		player.isHuy = true;
		this.sendToAll({game:{player:{ghe:player.map, info:{huy:true}}}});
		let huy = this.playerInGame.filter(function(t){return t.data.isHuy === false}); // ghế chưa hủy bài
		if (huy.length > 1 && this.game_player === player) {
			this.nextPlayer();
			return void 0;
		}
		if (huy.length === 1) {
			let noHuy = huy[0];
			noHuy.data.balans += noHuy.data.bet;
			let objWin = {ghe:noHuy.data.map, data:{balans:noHuy.data.balans}, info:{win:0}};
			let array = [objWin];
			this.playerInGame.forEach(function(obj, index){
				if (noHuy.data !== obj.data) {
					if (obj.data.bet <= noHuy.data.bet) {
						// ăn tất
						objWin.info.win += obj.data.bet;
						objWin.data.balans += obj.data.bet;
						noHuy.data.balans  += obj.data.bet;
						if (obj.data.isOut === false) {
							array = array.concat({ghe:obj.data.map, data:{balans:obj.data.balans}, info:{lost:obj.data.bet, stop:true}});
						}
					}else{
						// có trả lại
						objWin.info.win += noHuy.data.bet;
						objWin.data.balans += noHuy.data.bet;
						noHuy.data.balans  += noHuy.data.bet;
						if (obj.data.isOut) {
							obj.data.du = obj.data.bet-noHuy.data.bet;
							obj.data.tralai();
							obj.data.isPlay = false;
							obj.data.destroy();
						}else{
							obj.data.balans += obj.data.bet-noHuy.data.bet;
							array = array.concat({ghe:obj.data.map, data:{balans:obj.data.balans}, info:{lost:noHuy.data.bet, stop:true}});
						}
					}
				}
			});
			this.sendToAll({game:{info:{data:array}, stop:true}});
			this.resetGame();
		}
	}
}

Poker.prototype.resetGame = function(){
	this.resetData();
	this.online > 1 && this.checkGame();
}

Poker.prototype.resetData = function(){
	this.game_player  = null;
	this.isPlay       = false;
	clearTimeout(this.timeOut);
	clearInterval(this.regTimeStart);
	this.timeOut      = null;
	this.regTimeStart = null;
	this.card         = [];
	this.mainCard     = [];
}

Poker.prototype.destroy = function(){
	this.resetData();
	this.poker.removeRoom(this.game, this.singID);
}

// Tìm người chiến thắng
Poker.prototype.win = function(){
	let gamer = this.playerInGame.filter(function(t){return (t.data.isHuy === false && t.data.isOut === false)}); // lấy người chơi theo đến cùng
	gamer.forEach(function(player){
		let g_player = player.data;
		let card_concat = this.mainCard.concat(g_player.card);

		for (let i = 0; i < 7; i++) {
			let dataT = card_concat[i];
			// Lọc bài
			if (void 0 === g_player.boCard[dataT.card]) {
				g_player.boCard[dataT.card] = [dataT];
			}else{
				g_player.boCard[dataT.card] = g_player.boCard[dataT.card].concat(dataT);
			}
			// lọc chất
			if (void 0 === g_player.loc_chat[dataT.type]) {
				g_player.loc_chat[dataT.type] = [dataT];
			}else{
				g_player.loc_chat[dataT.type] = g_player.loc_chat[dataT.type].concat(dataT);
			}
			dataT = null;
		}
		// Lọc bỏ empty
		g_player.boCard = g_player.boCard.filter(function(el){
			return el != void 0;
		});
		// nếu có lớn hơn 5 lá bài khác nhau thì kiểm tra có dây hợp lệ không
		if (g_player.boCard.length > 4) {
			// Lấy ra các trường hợp của dây
			g_player.dequyDay(g_player.boCard, []);
			// Kiểm tra và suất ra kết quả cuối cùng
			g_player.checkDay();
		}else{
			// các trường hợp khác
			g_player.checkBo();
		}
	}.bind(this));
	// Lọc người chơi đang có bộ bài
	let player_bo = this.playerInGame.filter(function(player) {
		return player.data.caoNhat !== null;
	});
	if (player_bo.length > 0) {
		player_bo.sort(function(a, b){return b.data.caoNhat.code-a.data.caoNhat.code});
		let code = player_bo[0].data.caoNhat.code;
		player_bo = player_bo.filter(function(player) {
			return player.data.caoNhat.code === code;
		});
		if (player_bo.length > 0) {
			if (player_bo.length === 1) {
				this.winer(player_bo[0].data);
			}else{
				let card = null;
				switch(code){
					case 1:
						this.checkBaiLe(player_bo);
						break;
					case 2:
						player_bo.sort(function(a, b){return b.data.caoNhat.bo[0].card-a.data.caoNhat.bo[0].card});
						card = player_bo[player_bo.length-1].data.caoNhat.bo[0].card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bo[0].card : player_bo[0].data.caoNhat.bo[0].card;
						player_bo = player_bo.filter(function(player) {
							return player.data.caoNhat.bo[0].card === card;
						});
						if (player_bo.length === 1) {
							this.winer(player_bo[0].data);
						}else{
							this.checkBaiLe(player_bo, true);
						}
						break;
					case 3:
						player_bo.sort(function(a, b){return b.data.caoNhat.bo2A[0].card-a.data.caoNhat.bo2A[0].card});
						card = player_bo[player_bo.length-1].data.caoNhat.bo2A[0].card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bo2A[0].card : player_bo[0].data.caoNhat.bo2A[0].card;
						let player_bo2A = player_bo.filter(function(player) {
							return player.data.caoNhat.bo2A[0].card === card;
						});
						if (player_bo2A.length === 1) {
							this.winer(player_bo2A[0].data);
						}else{
							player_bo.sort(function(a, b){return b.data.caoNhat.bo2B[0].card-a.data.caoNhat.bo2B[0].card});
							card = player_bo[player_bo.length-1].data.caoNhat.bo2B[0].card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bo2B[0].card : player_bo[0].data.caoNhat.bo2B[0].card;
							player_bo = player_bo.filter(function(player) {
								return player.data.caoNhat.bo2B[0].card === card;
							});
							if (player_bo.length === 1) {
								this.winer(player_bo[0].data);
							}else{
								this.Hoa();
							}
						}
						break;
					case 4:
						player_bo.sort(function(a, b){return b.data.caoNhat.bo[0].card-a.data.caoNhat.bo[0].card});
						card = player_bo[player_bo.length-1].data.caoNhat.bo[0].card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bo[0].card : player_bo[0].data.caoNhat.bo[0].card;
						player_bo = player_bo.filter(function(player) {
							return player.data.caoNhat.bo[0].card === card;
						});
						if (player_bo.length === 1) {
							this.winer(player_bo[0].data);
						}else{
							this.checkBaiLe(player_bo, true);
						}
						break;
					case 5:
						player_bo.sort(function(a, b){return b.data.caoNhat.bai_cao.card-a.data.caoNhat.bai_cao.card});
						card = player_bo[player_bo.length-1].data.caoNhat.bai_cao.card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bai_cao.card : player_bo[0].data.caoNhat.bai_cao.card;
						player_bo = player_bo.filter(function(player) {
							return player.data.caoNhat.bai_cao.card === card;
						});
						if (player_bo.length === 1) {
							this.winer(player_bo[0].data);
						}else{
							this.checkBaiLe(player_bo, true);
						}
						break;
					case 6:
						player_bo.sort(function(a, b){return b.data.caoNhat.caonhat.card-a.data.caoNhat.caonhat.card});
						card = player_bo[player_bo.length-1].data.caoNhat.caonhat.card === 0 ? player_bo[player_bo.length-1].data.caoNhat.caonhat.card : player_bo[0].data.caoNhat.caonhat.card;
						player_bo = player_bo.filter(function(player) {
							return player.data.caoNhat.caonhat.card === card;
						});
						if (player_bo.length === 1) {
							this.winer(player_bo[0].data);
						}else{
							this.checkBaiLe(player_bo, true);
						}
						break;
					case 7:
						player_bo.sort(function(a, b){return b.data.caoNhat.bo3[0].card-a.data.caoNhat.bo3[0].card});
						card = player_bo[player_bo.length-1].data.caoNhat.bo3[0].card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bo3[0].card : player_bo[0].data.caoNhat.bo3[0].card;
						let player_bo3 = player_bo.filter(function(player) {
							return player.data.caoNhat.bo3[0].card === card;
						});
						if (player_bo3.length === 1) {
							this.winer(player_bo3[0].data);
						}else{
							player_bo.sort(function(a, b){return b.data.caoNhat.bo2[0].card-a.data.caoNhat.bo2[0].card});
							card = player_bo[player_bo.length-1].data.caoNhat.bo2[0].card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bo2[0].card : player_bo[0].data.caoNhat.bo2[0].card;
							player_bo = player_bo.filter(function(player) {
								return player.data.caoNhat.bo2[0].card === card;
							});
							if (player_bo.length === 1) {
								this.winer(player_bo[0].data);
							}else{
								this.Hoa();
							}
						}
						break;
					case 8:
						player_bo.sort(function(a, b){return b.data.caoNhat.bo[0].card-a.data.caoNhat.bo[0].card});
						card = player_bo[player_bo.length-1].data.caoNhat.bo[0].card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bo[0].card : player_bo[0].data.caoNhat.bo[0].card;
						player_bo = player_bo.filter(function(player) {
							return player.data.caoNhat.bo[0].card === card;
						});
						if (player_bo.length === 1) {
							this.winer(player_bo[0].data);
						}else{
							this.Hoa();
						}
						break;
					case 9:
						player_bo.sort(function(a, b){return b.data.caoNhat.bai_cao.card-a.data.caoNhat.bai_cao.card});
						card = player_bo[player_bo.length-1].data.caoNhat.bai_cao.card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bai_cao.card : player_bo[0].data.caoNhat.bai_cao.card;
						player_bo = player_bo.filter(function(player) {
							return player.data.caoNhat.bai_cao.card === card;
						});
						if (player_bo.length === 1) {
							this.winer(player_bo[0].data);
						}else{
							this.checkBaiLe(player_bo, true);
						}
						break;
					case 10:
						player_bo.sort(function(a, b){return b.data.caoNhat.bai_cao.card-a.data.caoNhat.bai_cao.card});
						card = player_bo[player_bo.length-1].data.caoNhat.bai_cao.card === 0 ? player_bo[player_bo.length-1].data.caoNhat.bai_cao.card : player_bo[0].data.caoNhat.bai_cao.card;
						player_bo = player_bo.filter(function(player) {
							return player.data.caoNhat.bai_cao.card === card;
						});
						if (player_bo.length === 1) {
							this.winer(player_bo[0].data);
						}else{
							this.checkBaiLe(player_bo, true);
						}
						break;
				}
			}
		}
	}else{
		this.Hoa();
	}
}
// kiểm tra bài lẻ
Poker.prototype.checkBaiLe = function(player_bo, add = false){
	player_bo.sort(function(a, b){return b.data.caoNhat.cao.card-a.data.caoNhat.cao.card});
	let card = player_bo[player_bo.length-1].data.caoNhat.cao.card === 0 ? player_bo[player_bo.length-1].data.caoNhat.cao : player_bo[0].data.caoNhat.cao;
	player_bo = player_bo.filter(function(player) {
		return player.data.caoNhat.cao.card === card.card;
	});
	if (player_bo.length === 1) {
		player_bo = player_bo[0].data;
		if (add) {
			player_bo.caoNhat.bo = [...player_bo.caoNhat.bo, card];
		}
		this.winer(player_bo);
	}else{
		player_bo.sort(function(a, b){return b.data.caoNhat.thap.card-a.data.caoNhat.thap.card});
		card = player_bo[player_bo.length-1].data.caoNhat.thap.card === 0 ? player_bo[player_bo.length-1].data.caoNhat.thap : player_bo[0].data.caoNhat.thap;
		player_bo = player_bo.filter(function(player) {
			return player.data.caoNhat.thap.card === card.card;
		});
		if (player_bo.length === 1) {
			player_bo = player_bo[0].data;
			if (add) {
				player_bo.caoNhat.bo = [...player_bo.caoNhat.bo, card];
			}
			this.winer(player_bo);
		}else{
			this.Hoa();
		}
	}
}
// Kết thúc game và Đã tìm ra người chiến thắng
Poker.prototype.winer = function(player){
	player.balans += player.bet;
	let objWin = {ghe:player.map, data:{balans:player.balans, openCard:player.card}, info:{win:0}};
	let array = [objWin];

	this.playerInGame.forEach(function(obj, index){
		if (player !== obj.data && obj.data.bet > 0) {
			if (obj.data.bet <= player.bet) {
				// ăn tất
				let an = (obj.data.bet*0.98)>>0; // trừ phế 2%
				objWin.info.win += an;
				objWin.data.balans += an;
				player.balans      += an;
				if (obj.data.isOut === false) {
					if (obj.data.isHuy) {
						array = array.concat({ghe:obj.data.map, data:{balans:obj.data.balans}, info:{lost:obj.data.bet}});
					}else{
						array = array.concat({ghe:obj.data.map, data:{balans:obj.data.balans, openCard:obj.data.card}, info:{lost:obj.data.bet}});
					}
				}
			}else{
				// có trả lại
				let an = (player.bet*0.98)>>0; // trừ phế 2%
				objWin.info.win += an;
				objWin.data.balans += an;
				player.balans      += an;
				if (obj.data.isOut) {
					obj.data.du = obj.data.bet-player.bet;
					obj.data.tralai();
					obj.data.isPlay = false;
					obj.data.destroy();
				}else{
					obj.data.balans += obj.data.bet-player.bet;
					if (obj.data.isHuy) {
						array = array.concat({ghe:obj.data.map, data:{balans:obj.data.balans}, info:{lost:player.bet}});
					}else{
						array = array.concat({ghe:obj.data.map, data:{balans:obj.data.balans, openCard:obj.data.card}, info:{lost:player.bet}});
					}
				}
			}
		}
	});
	this.sendToAll({game:{info:{data:array, win:{ghe:player.map, bo:player.caoNhat.bo, code:player.caoNhat.code}}, finish:true}});
	this.resetGame();
}
// Kết thúc game nhưng Hòa game
Poker.prototype.Hoa = function(){
	let array = [];
	this.playerInGame.forEach(function(obj, index){
		obj.data.balans += obj.data.bet;
		if (obj.data.isOut) {
			obj.data.du = obj.data.bet;
			obj.data.tralai();
			obj.data.isPlay  = false;
			obj.data.destroy();
		}else{
			array = array.concat({ghe:obj.data.map, data:{balans:obj.data.balans, openCard:obj.data.card}, info:{hoa:obj.data.bet}});
		}
	});
	this.sendToAll({game:{info:{data:array}, finish:true}});
	this.resetGame();
}

module.exports = Poker;
