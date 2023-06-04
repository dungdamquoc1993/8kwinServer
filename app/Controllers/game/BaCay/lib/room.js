
let UserInfo  = require('../../../../Models/UserInfo');
var base_card = require('../../../../../data/card');
var Helpers   = require('../../../../Helpers/Helpers');

var BaCay = function(bacay, singID, game){
	this.bacay  = bacay;  // quản lý các phòng
	this.singID = singID; // ID phòng
	this.game   = game;   // game (100/1000/5000/10000/...)
	bacay.addRoom(this);

	this.online   = 0;    // số người trong phòng
	this.card     = [];   // bộ bài

	// ghế ngồi có sẵn 
	this.player = {
		1: null,
		2: null,
		3: null,
		4: null,
		5: null,
		6: null,
	};

	this.playerInGame = []; // đang chơi

	this.isPlay       = false; // phòng đang chơi

	this.timeOut      = null;  // thời gian
	this.regTimeStart = null;  // Đăng ký thời gian bắt đầu

	this.chuongNew    = null;
	this.chuong       = null;  // Cầm chương

	this.bet_truong   = 0;   // tổng cược chương
	this.bet_ga       = 0;   // tổng cược Gà

	this.game_time      = 0;    // mini time

	this.timePlayerCuoc = 15;   // thời gian người chơi đặt cược
	this.time_player    = 0;    // thời gian còn lại của các người chơi

	this.game_round     = 0;    // round game

	//this.game_start    = false; // game đã bắt đầu

	this.timeStartGame = 5;   // thời gian bắt đầu game
	this.time_start    = 0;   // thời gian bắt đầu game

	this.sendToAll = function(data, player = null){
		Object.values(this.player).forEach(function(obj){
			if (!!obj && obj !== player && !!obj.client) {
				obj.client.red(data);
			}
		});
	}

	// Có người vào phòng
	this.inroom = function(player){
		this.online++;

		if (this.online > 5) {
			this.bacay.removeRoom(this.game, this.singID);
		}else{
			this.bacay.addRoom(this);
		}

		player.room = this;
		let trongPhong = Object.entries(this.player); // danh sách ghế
		let gheTrong = trongPhong.filter(function(t){return t[1] == null}); // lấy các ghế trống

		// lấy ngẫu nhiên 1 ghế trống và ngồi
		let rand = (Math.random()*gheTrong.length)>>0;
		gheTrong = gheTrong[rand];

		this.player[gheTrong[0]] = player; // ngồi
		player.map = gheTrong[0];          // vị trí ngồi

		trongPhong = Object.entries(this.player);

		this.sendToAll({ingame:{ghe:player.map, data:{name:player.name, avatar:player.avatar, balans:player.balans}}}, player);

		let card = [];
		let client = {infoRoom:{game:this.game, isPlay:this.isPlay, time_start:this.time_start, betGa:this.bet_ga}, meMap:player.map, game:{}};
		if (this.game_round == 1) {
			client.infoRoom.time = this.time_player;
			client.infoRoom.round = this.game_round;
		}
		let result = trongPhong.map(function(ghe){
			if (!!ghe[1]) {
				let data = {ghe:ghe[0], data:{name:ghe[1].name, avatar:ghe[1].avatar, balans:ghe[1].balans}};
				if (ghe[1].isPlay) {
					if (this.game_round == 1) {
						data.data.betChuong = ghe[1].betChuong;
						data.data.betGa     = ghe[1].betGa;
						data.data.progress  = this.time_player+1;
						data.data.round     = 1;
					}else if (this.game_round == 2) {
						data.data.betChuong = ghe[1].betChuong;
						data.data.betGa     = ghe[1].betGa;
						card = card.concat({ghe:ghe[0], card:{}});
					}
				}
				return data;
			}else{
				return {ghe:ghe[0], data:null};
			}
		}.bind(this));
		client.infoGhe = result;
		client.infoRoom.card = card;
		if (this.chuong) {
			client.game.truong = this.chuong.map;
		}

		player.client.red(client);

		this.online > 1 && this.checkGame(5000);
	}

	// Có người thoát khỏi phòng
	this.checkOutRoom = function(player){
		if (this.game_round == 0 || !player.isPlay) {
			if (player.client) {
				player.client.red({kick:true});
				player.client.bacay = null;
			}
			delete process.redT.game.bacay.player[player.uid];
			player.client = null;
			player.room = null;
			this.outroom(player);
		}else{
			this.sendToAll({game:{regOut:{map:player.map, reg:true}}});
		}
		player = null;
	}
	// Có người thoát khỏi phòng
	this.outroom = function(player){
		this.online--;
		this.player[player.map] = null;
		if (this.chuongNew === player) {
			this.chuongNew = null;
		}
		if (this.chuong === player) {
			this.chuong = null;
			this.online > 0 && this.chuongThoat();
		}
		if (this.online < 1) {
			this.destroy();
		}else{
			this.sendToAll({outgame:player.map});
			if (this.online == 1) {
				this.resetData();
			}
		}
		player = null;
	}

	this.resetGame = function(){
		this.resetData();
		this.online > 1 && this.checkGame();
	}

	// Chương thoát, dừng game và trả lại tiền cược
	this.chuongThoat = function(){
		// chuyển chương
		let nguoichoi = Object.values(this.player).filter(function(t){return t !== null});
		this.chuong = nguoichoi[(Math.random()*nguoichoi.length)>>0];
		this.sendToAll({game:{truong:this.chuong.map}});

		if(this.isPlay && this.game_round == 1){
			// trả lại tiền cược
		}
	}
	// dừng game, trả lại tiền nếu đang đặt
	this.stopGame = function(){
	}
	// Đặt lại dữ liệu phòng
	this.resetData = function(){
		this.game_round   = 0;     // round game
		this.bet_truong   = 0;     // tổng cược chương
		this.bet_ga       = 0;     // tổng cược Gà
		this.isPlay       = false;
		clearTimeout(this.timeOut);
		clearInterval(this.regTimeStart);
		this.timeOut      = null;
		this.regTimeStart = null;
		this.card         = [];
		this.playerInGame = [];
		Object.values(this.player).forEach(function(player){
			if (!!player) {
				!!player && player.newGame();
				if (player.regOut) {
					setTimeout(function(){
						player.outGame(true);
						player = null;
					}, 2000);
				}
			}
		}.bind(this));
	}

	// Phá hủy phòng
	this.destroy = function(){
		this.resetData();
		this.bacay.removeRoom(this.game, this.singID);
	}

	// Kiểm tra và bắt đầu chơi
	this.checkGame = function(tru = 0){
		if (!this.isPlay && !this.timeOut) {
			this.isPlay  = true;
			this.timeOut = setTimeout(function(){
				clearTimeout(this.timeOut);
				this.timeOut = null;
				this.time_start = this.timeStartGame;
				// danh sách người chơi
				let nguoichoi = Object.values(this.player).filter(function(t){return t !== null});
				let arrPromise = [];
				nguoichoi.forEach(function(player){
					if (player.regOut) {
						// kíck
						player.outGame(true);
					}else{
						player.newGame();
						arrPromise.push(UserInfo.findOne({id:player.uid}, 'red').exec());
					}
				}.bind(this));
				// kiểm tra đủ tiền ở lại bàn
				Promise.all(arrPromise).then(results => {
					results.forEach(function(obj, index){
						if (obj.red < 4*this.game){
							nguoichoi[index].outGame(true)
						}
					}.bind(this));
					// tải lại danh sách người chơi
					nguoichoi = Object.values(this.player).filter(function(t){return t !== null});
					if (nguoichoi.length < 2) {
						this.isPlay = false;
						return void 0;
					}

					// đặt chương mới
					if (!!this.chuongNew){
						this.chuong = this.chuongNew;
					}else if(!this.chuong){
						this.chuong = nguoichoi[(Math.random()*nguoichoi.length)>>0];
					}
					this.chuongNew = null;
					this.sendToAll({infoRoom:{time_start:this.timeStartGame, isPlay:true}, game:{truong:this.chuong.map}});

					this.regTimeStart = setInterval(function(){
						if (this.time_start < 0) {
							clearInterval(this.regTimeStart);
							// ghế có người ngồi
							nguoichoi = Object.values(this.player).filter(function(t){return t !== null});
							if (nguoichoi.length < 2) {
								this.isPlay = false;
								return void 0;
							}
							this.playerInGame = nguoichoi.map(function(player){
								player.isPlay = true;
								return player;
							});
							this.Round1();
						}
						this.time_start--;
					}.bind(this), 1000);
				});
			}.bind(this), 8000-tru);
		}
	}

	// đặt cược
	this.Round1 = function(){
		this.game_round = 1; // round game
		this.time_player = this.timePlayerCuoc;

		// danh sách người chơi được đặt cược
		let listPlayer = this.playerInGame.map(function(player){
			return {map:player.map, progress:this.time_player+1, round:1};
		}.bind(this));

		this.sendToAll({infoRoom:{time:this.time_player, round:this.game_round}, game:{listPlayer:listPlayer}});

		this.regTimeStart = setInterval(function(){
			if (this.time_player < 0) {
				clearInterval(this.regTimeStart);
				// hết thời gian đặt cược, tự cược với mức tiền của phòng
				this.playerInGame.forEach(function(player){
					if (player !== this.chuong && player.betChuong === 0) {
						player.cuocChuong(player.game);
					}
				}.bind(this));

				if (this.playerInGame.length < 2) {
					// game dừng và trả lại tiền cược
				}else{
					// tiếp tục vòng 2
					this.Round2();
				}
			}
			this.time_player--;
		}.bind(this), 1000);
	}

	// chia bài
	this.Round2 = function(){
		this.game_round = 2;    // round game
		this.card = [...base_card.card]; // bộ bài mới
		this.card = this.card.splice(0, 36);
		this.card = Helpers.shuffle(this.card); // tráo bài lần 1
		this.card = Helpers.shuffle(this.card); // tráo bài lần 2
		this.card = Helpers.shuffle(this.card); // tráo bài lần 3
		// chia bài
		let chia = [];

		this.playerInGame.forEach(function(player, index){
			player.card = this.card.splice(0, 3);
			chia[index] = {map:player.map};
			let temp1 = player.card.map(function(card, i){
				player.point += card.card+1;
				return {card:card.card, type:card.type == 1 ? 5 : (card.type == 0 ? 4 : card.type)};
			});
			// sắp xếp chất
			temp1.sort(function(a, b){
				return b.type-a.type;
			});
			// chất to nhất
			let chat = temp1.filter(function(t){return t.type == temp1[0].type});
			chat.sort(function(a, b){
				return b.card-a.card;
			});
			player.toNhat = chat[0];
			if (chat[0].type == 5 && chat[chat.length-1].card == 0) {
				player.toNhat = chat[chat.length-1];
			}
			player.point = player.point%10;
			player.point = player.point === 0 ? 10 : player.point;
		}.bind(this));

		this.playerInGame.forEach(function(player){
			chia.forEach(function(dataChia){
				if (dataChia.map == player.map) {
					dataChia.data = player.card;
				}else{
					delete dataChia.data;
				}
			});
			player.client !== null && player.client.red({game:{chia_bai:chia, btn_lat:true}});
		}.bind(this));

		clearTimeout(this.timeOut);
		this.timeOut = setTimeout(function(){
			clearTimeout(this.timeOut);
			this.Round3();
		}.bind(this), 12000);
	}

	// Tính điểm
	this.Round3 = function(){
		this.game_round = 3;    // round game
		UserInfo.findOne({id:this.chuong.uid}, 'red').exec(function(err, truong){
			if (!!truong) {
				if (truong.red >= this.bet_truong) {
					// danh sách người chơi có điểm cao hơn chương
					let cao_chuong = this.playerInGame.filter(function(t){
						return t.point > this.chuong.point;
					}.bind(this));

					// trả thưởng cho người chơi cao hơn chương
					cao_chuong.length > 0 && this.cao_chuong(cao_chuong);

					// danh sách người chơi có điểm bằng chương
					let bang_chuong = this.playerInGame.filter(function(t){
						return t.point == this.chuong.point;
					}.bind(this));
					bang_chuong.length > 1 && this.bang_chuong(bang_chuong);

					// danh sách người chơi có điểm thấp hơn chương
					let thap_chuong = this.playerInGame.filter(function(t){
						return t.point < this.chuong.point;
					}.bind(this));

					thap_chuong.length > 0 && this.thap_chuong(thap_chuong);
					// Kết thúc tính điểm với chương

					// Tính điểm 10 vào cao hơn chương làm chương
					// Danh sách người có điểm 10
					let list10 = this.playerInGame.filter(function(t){
						return t.point == 10;
					}.bind(this));

					if (list10.length > 1) {
						// sắp sếp theo chất
						list10.sort(function(player_a, player_b){
							return player_b.toNhat.type-player_a.toNhat.type;
						});
						// chất to nhất
						let chat = list10.filter(function(t){return t.toNhat.type == list10[0].toNhat.type});
						chat.sort(function(player_a, player_b){
							return player_b.toNhat.card-player_a.toNhat.card;
						});

						// tìm ra chương mới
						list10 = chat[0];
						if (chat[0].toNhat.type == 5 && chat[chat.length-1].toNhat.card == 0) {
							list10 = chat[chat.length-1];
						}
						if (list10 !== this.chuong) {
							this.chuongNew = list10;
						}
					}else if (list10.length == 1 && list10[0] !== this.chuong) {
						this.chuongNew = list10[0];
					}
					// kết thúc tìm chương

					// danh sách người chơi đánh Gà
					let gamer_ga = this.playerInGame.filter(function(t){return t.betGa > 0}); // lấy người chơi đánh Gà
					if (gamer_ga.length > 1) {
						gamer_ga.sort(function(player_a, player_b){
							return player_b.point-player_a.point;
						});
						let top_gamer_ga = this.playerInGame.filter(function(t){return t.point == gamer_ga[0].point});
						if (top_gamer_ga.length > 1) {
							// có nhiều người bằng điểm, tính chất
							// sắp xếp chất
							top_gamer_ga.sort(function(player_a, player_b){
								return player_b.toNhat.type-player_a.toNhat.type;
							});
							// chất to nhất
							let chat = top_gamer_ga.filter(function(t){return t.toNhat.type == top_gamer_ga[0].toNhat.type});
							chat.sort(function(player_a, player_b){
								return player_b.toNhat.card-player_a.toNhat.card;
							});

							// tìm ra người ăn gà
							top_gamer_ga = chat[0];
							if (chat[0].toNhat.type == 5 && chat[chat.length-1].toNhat.card == 0) {
								top_gamer_ga = chat[chat.length-1];
							}
						}else{
							// tìm ra người ăn gà
							top_gamer_ga = top_gamer_ga[0];
						}
						gamer_ga.forEach(function(player){
							if (player !== top_gamer_ga) {
								player.totall -= player.betGa;
							}
						}.bind(this));
						top_gamer_ga.totall += this.bet_ga-top_gamer_ga.betGa;
						top_gamer_ga.balans += this.bet_ga;
						let bet_ga = this.bet_ga;
						UserInfo.findOneAndUpdate({id:top_gamer_ga.uid}, {$inc:{red:this.bet_ga}}).exec(function(err, user){
							if (!!user) {
								top_gamer_ga.balans = user.red*1+bet_ga;
							}
							bet_ga = null;
							top_gamer_ga = null;
						}.bind(this));
					}else if (gamer_ga.length == 1) {
						// trả lại Gà khi chỉ có 1 người cược
						gamer_ga = gamer_ga[0];
						gamer_ga.balans += gamer_ga.betGa;
						UserInfo.findOneAndUpdate({id:gamer_ga.uid}, {$inc:{red:gamer_ga.betGa}}).exec(function(err, user){
							if (!!user) {
								gamer_ga.balans = user.red*1+gamer_ga.betGa;
							}
							gamer_ga = null;
						});
					}
					// Gửi thông tin thắng thua
					let data = this.playerInGame.map(function(player){
						let player_g = {map:player.map};
						player_g.openCard = {card:player.card, point:player.point};
						player_g.totall = player.totall;
						player_g.balans = player.balans;
						return player_g;
					});
					this.sendToAll({game:{done:data}});
					this.resetGame();
				}else{
					// trả lại (Chương không đủ trả)
					this.chuong = null; // tước quyền chương
					//console.log('trả lại (Chương không đủ trả)');
					// trả lại tiền
					this.playerInGame.forEach(function(player){
						let balans = player.betGa+player.betChuong;
						if(balans > 0){
							player.balans += balans;
							UserInfo.findOneAndUpdate({id:player.uid}, {$inc:{red:balans}}).exec(function(err, user){
								if (!!user) {
									player.balans = user.red*1+player.betGa;
								}
								player = null;
							});
						}
					}.bind(this));
					let listPlayer = Object.values(this.player).filter(function(t){return t !== null});
					listPlayer = listPlayer.map(function(player){
						return {map:player.map, balans:player.balans};
					});
					this.sendToAll({game:{stop:0, listPlayer:listPlayer}});
					this.resetGame();
				}
			}else{
				// trả lại (Chương không tồn tại trong cơ sở dữ liệu)
				this.chuong = null; // tước quyền chương
				//console.log('trả lại (Chương không tồn tại trong cơ sở dữ liệu)');
				// trả lại tiền
				this.playerInGame.forEach(function(player){
					let balans = player.betGa+player.betChuong;
					if(balans > 0){
						player.balans += balans;
						UserInfo.findOneAndUpdate({id:player.uid}, {$inc:{red:balans}}).exec(function(err, user){
							if (!!user) {
								player.balans = user.red*1+player.betGa;
							}
							player = null;
						});
					}
				}.bind(this));
				let listPlayer = Object.values(this.player).filter(function(t){return t !== null});
				listPlayer = listPlayer.map(function(player){
					return {map:player.map, balans:player.balans};
				});
				this.sendToAll({game:{stop:0, listPlayer:listPlayer}});
				this.resetGame();
			}
		}.bind(this));
	};

	// cao hơn chương ăn
	this.cao_chuong = function(list){
		let totall = 0; // tổng tiền chương phải trả
		list.forEach(function(player){
			if (player.betChuong > 0) {
				let an = (player.betChuong*0.98)>>0; // trừ phế 2%
				let win = an+player.betChuong;
				totall += player.betChuong;
				player.totall += an;
				player.balans += win;
				UserInfo.findOneAndUpdate({id:player.uid}, {$inc:{red:win}}).exec(function(err, user){
					if (!!user) {
						player.balans = user.red*1+win;
					}
					player = null;
					win    = null;
				});
			}
		});
		if (totall > 0) {
			this.chuong.balans -= totall;
			this.chuong.totall -= totall;
			UserInfo.findOneAndUpdate({id:this.chuong.uid}, {$inc:{red:-totall}}).exec(function(err, user){
				if (!!user) {
					this.chuong.balans = user.red-totall;
				}
				totall = null;
			}.bind(this));
		}
	};

	// Bằng điểm chương tính chất
	this.bang_chuong = function(list){
		// danh sách người chơi có chất cao hơn chương
		let cao_chuong = list.filter(function(t){
			return t.toNhat.type > this.chuong.toNhat.type;
		}.bind(this));
		// trả thưởng cho người chơi cao hơn chương
		cao_chuong.length > 0 && this.cao_chuong(cao_chuong);

		// danh sách người chơi có chất bằng chương
		let bang_chuong = list.filter(function(t){
			return t.toNhat.type == this.chuong.toNhat.type;
		}.bind(this));

		// tiếp tục so sánh người chơi có cùng điểm, cùng chất với chương
		if (bang_chuong.length > 1) {
			// nếu là chất dô thì đặt A là 13
			if (this.chuong.toNhat.type == 5){
				bang_chuong.forEach(function(player){
					if (player.toNhat.card == 0) {
						player.toNhat.card = 13;
					}
				});
			}
			bang_chuong.sort(function(player_a, player_b){
				return player_b.toNhat.card-player_a.toNhat.card;
			});
			let index_chuong = bang_chuong.indexOf(this.chuong);
			if (index_chuong >= 0) {
				let cao  = bang_chuong.slice(0, index_chuong);
				cao.length > 0 && this.cao_chuong(cao);
				let thap = bang_chuong.slice(index_chuong+1, bang_chuong.length);
				thap.length > 0 && this.thap_chuong(thap);
			}
		}

		// danh sách người chơi có chất thấp hơn chương
		let thap_chuong = list.filter(function(t){
			return t.toNhat.type < this.chuong.toNhat.type;
		}.bind(this));
		// thu tiền người chơi thấp hơn chương
		thap_chuong.length > 0 && this.thap_chuong(thap_chuong);
	};

	// chương ăn thấp hơn
	this.thap_chuong = function(list){
		let totall = 0; // tổng tiền chương ăn
		list.forEach(function(player){
			if (player.betChuong > 0) {
				totall += (player.betChuong*0.98)>>0; // trừ phế 2%
				player.totall -= player.betChuong;
			}
		});
		if (totall > 0) {
			this.chuong.balans += totall;
			this.chuong.totall += totall;
			UserInfo.findOneAndUpdate({id:this.chuong.uid}, {$inc:{red:totall}}).exec(function(err, user){
				if (!!user){
					this.chuong.balans = user.red*1+totall;
				}
				totall = null;
			}.bind(this));
		}
	};
};

module.exports = BaCay;
