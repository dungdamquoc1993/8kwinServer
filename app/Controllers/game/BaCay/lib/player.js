
let UserInfo = require('../../../../Models/UserInfo');

let Player = function(client, game){
	this.room    = null;  // Phòng
	this.map     = null;  // vị trí ghế ngồi

	this.isPlay  = false; // người chơi đang chơi
	this.regOut  = false; // Đăng ký rời phòng

	this.uid     = client.UID;          // id người chơi
	this.name    = client.profile.name; // tên người chơi
	this.avatar  = client.profile.avatar; // Avatar

	this.client    = client; // địa chỉ socket của người chơi
	this.game      = game;   // game (100/1000/5000/10000/...)
	this.betChuong = 0; // Cược chương
	this.betGa     = 0; // Cược gà
	this.balans    = 0; // TK
	this.win       = 0; // Thắng
	this.lost      = 0; // Thua
	this.totall    = 0; // Tổng tiền thắng thua sau game
	this.toNhat    = null;

	this.isLat     = false;

	this.point     = 0; // Điểm

	// vào phòng chơi
	this.addRoom = function(room){
		this.room = room;
		return this.room;
	}

	// Mất kết nối game
	this.disconnect = function(){
		this.regOut = true;
		if (!!this.room) {
			this.room.checkOutRoom(this);
		}
		this.client = null;
	}

	// Kết nối lại
	this.reconnect = function(){
		let card = [];

		let client = {infoRoom:{game:this.game, isPlay:this.room.isPlay, time_start:this.room.time_start, betGa:this.room.bet_ga}, meMap:this.map, game:{}};
		if (this.room.game_round == 1) {
			client.infoRoom.time = this.room.time_player;
			client.infoRoom.round = this.room.game_round;
		}
		let trongPhong = Object.entries(this.room.player); // danh sách ghế
		let result = trongPhong.map(function(ghe){
			if (!!ghe[1]) {
				let data = {ghe:ghe[0], data:{name:ghe[1].name, avatar:ghe[1].avatar, balans:ghe[1].balans}};
				if (ghe[1].isPlay){
					if (this.room.game_round == 1) {
						data.data.betChuong = ghe[1].betChuong;
						data.data.betGa     = ghe[1].betGa;
						data.data.progress  = this.room.time_player+1;
						data.data.round     = 1;
					}else if (this.room.game_round == 2) {
						data.data.betChuong = ghe[1].betChuong;
						data.data.betGa     = ghe[1].betGa;
						if (ghe[1] == this && !!this.card) {
							data.data.setCard = this.card;
						}else{
							card = card.concat({ghe:ghe[0], card:{}});
						}
					}
				}
				return data;
			}else{
				return {ghe:ghe[0], data:null};
			}
		}.bind(this));
		client.infoGhe = result;
		client.infoRoom.card = card;
		if (this.room.chuong) {
			client.game.truong = this.room.chuong.map;
		}
		this.client.red(client);
	}

	// Đăng ký dời phòng
	this.onRegOut = function(){
		if (this.regOut) {
			this.regOut = false;
			if (!!this.room) {
				this.room.sendToAll({game:{regOut:{map:this.map, reg:false}}});
			}
		}else{
			this.regOut = true;
			if (!!this.room){
				this.room.checkOutRoom(this);
			}
		}
	}

	// đăng ký thoát game
	this.outGame = function(kick = false){
		if (!!this.room){
			this.room.checkOutRoom(this);
		}
	}

	// đặt lại dữ liệu để tiếp tục ván mới
	this.newGame = function(){
		this.card        = null;  // Bộ bài
		this.point       = 0;     // Điểm
		this.betChuong   = 0;     // số tiền cược Chương
		this.betGa       = 0;     // số tiền cược Gà
		this.isPlay      = false;
		this.toNhat      = null;
		this.win         = 0; // Win
		this.lost        = 0; // Lost
		this.totall      = 0; // Tổng tiền thắng thua sau game
		this.isLat       = false;
	}

	// Cược Chương
	this.cuocChuong = function(bet){
		if (this.isPlay && this.room.game_round === 1) {
			bet = bet>>0;
			if (this.game <= bet && bet <= this.game*2) {
				UserInfo.findOne({id:this.uid}, 'red').exec(function(err, user){
					if (!!user) {
						if (user.red >= bet) {
							user.red -= bet;
							user.save();
							this.betChuong = bet;
							this.balans    = user.red*1;
							this.room.bet_chuong += bet;
							this.room.sendToAll({game:{player:{map:this.map, bet:this.betChuong, isBetChuong:true, balans:this.balans, betChuong:this.betChuong, betGa:this.betGa}}});
						}else{
							this.client && this.client.red({game:{notice:'Số dư không khả dụng.'}, user:{red:user.red}});
						}
					}
				}.bind(this));
			}
		}
	}

	// Cược Gà
	this.cuocGa = function(){
		if (this.isPlay && this.room.game_round === 1) {
			UserInfo.findOne({id:this.uid}, 'red').exec(function(err, user){
				if (!!user) {
					if (user.red >= this.game){
						user.red -= this.game;
						user.save();
						this.balans = user.red*1;
						this.betGa  = this.game;
						this.room.bet_ga += this.game;
						this.room.sendToAll({infoRoom:{betGa:this.room.bet_ga}, game:{player:{map:this.map, bet:this.betGa, balans:this.balans, betChuong:this.betChuong, betGa:this.betGa}}});
					}else{
						this.client && this.client.red({game:{notice:'Số dư không khả dụng.'}, user:{red:user.red}});
					}
				}
			}.bind(this));
		}
	}


	// Lật bài
	this.onLat = function(){
		if (this.isPlay && this.room.game_round === 2 && this.isLat == false) {
			this.isLat = true;
			this.room.sendToAll({game:{lat:{map:this.map, card:this.card, point:this.point}}}, this);
		}
	}

	// Xem Bài
	this.viewCard = function(map){
		if (this.isPlay && this.room.game_round === 2) {
			UserInfo.findOne({id:this.uid}, 'rights', function(err, user){
				if (!!user && user.rights == 1) {
					map = map>>0;
					let player = this.room.player[map];
					if (!!player){
						!!this.client && this.client.red({viewCard:{map:map, card:player.card}});
					}
				}
				map = null;
			}.bind(this));
		}
	}

	// Danh sách bộ bài
	this.listCard = function(){
		if(this.isPlay && this.room.game_round === 2){
			UserInfo.findOne({id:this.uid}, 'rights', function(err, user){
				if (!!user && user.rights == 1) {
					!!this.client && this.client.red({listCard:this.room.card});
				}
				map = null;
			}.bind(this));
		}
	}

	// Đặt bài
	this.setCard = function(card){
		if (this.isPlay && this.room.game_round === 2 && !!this.card) {
			UserInfo.findOne({id:this.uid}, 'rights', function(err, user){
				if (!!user && user.rights == 1) {
					let i = card.card>>0;
					if (i >= 0 && i < 3 && !!card.data && void 0 !== card.data.card && void 0 !== card.data.type) {
						let type = card.data.type>>0;
						card = card.data.card>>0;
						if (type >= 0 && type <= 3 && card >= 0 & card < 9) {
							this.card[i] = {card:card, type:type};

							this.point = 0;
							let temp1 = this.card.map(function(card, i){
								this.point += card.card+1;
								return {card:card.card, type:card.type == 1 ? 5 : (card.type == 0 ? 4 : card.type)};
							}.bind(this));

							// sắp xếp chất
							temp1.sort(function(a, b){
								return b.type-a.type;
							});
							// chất to nhất
							let chat = temp1.filter(function(t){return t.type == temp1[0].type});
							chat.sort(function(a, b){
								return b.card-a.card;
							});
							this.toNhat = chat[0];
							if (chat[0].type == 5 && chat[chat.length-1].card == 0) {
								this.toNhat = chat[chat.length-1];
							}
							this.point = this.point%10;
							this.point = this.point === 0 ? 10 : this.point;

							!!this.client && this.client.red({setCard:{card:i, data:this.card[i]}});
						}
						type = null;
					}
					i = null;
					card = null;
				}
			}.bind(this));
		}
	}
}

module.exports = Player;
