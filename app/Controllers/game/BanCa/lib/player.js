
var UserInfo = require('../../../../Models/UserInfo');
var History  = require('../../../../Models/BanCa/BanCa_history');

var Player = function(client, game, money){
	this.room   = null;       // Phòng game
	this.map    = null;       // vị trí ghế ngồi
	this.uid    = client.UID; // id người chơi
	this.client = client;     // địa chỉ socket của người chơi
	this.game   = game;       // game (1/2/3)

	this.bet     = 0;         // Tiền mỗi viên đạn
	this.typeBet = 0;         // loại dạn (0: mặc định)

	this.fishTotal  = 0;      // Cá đã bắn được
	this.moneyTotal = money;  // Tổng tiền mang vào
	this.money      = money;  // số tiền chơi

	this.meBulllet = {};

	this.isPlay = false;
	this.scene  = false;
}


Player.prototype.collision = function(data){
	let bullet_id = data.id>>0;
	let fish_id   = data.f>>0;

	let bullet = this.meBulllet[bullet_id];
	let fish   = this.room.fish[fish_id];

	if (void 0 !== bullet) {
		delete this.meBulllet[bullet_id];
		if (void 0 !== fish) {
			fish.coll[bullet.type]--;
			if (fish.coll[bullet.type] < 1) {
				delete this.room.fish[fish_id];
				let money = bullet.bet*this.room.root.fish[fish.f].b;
				this.money += money;
				this.room.sendToAll({otherEat:{id:fish_id, money:money, map:this.map, m:this.money}}, this);
				this.client.red({meEat:{id:fish_id, money:money, m:this.money}});
				this.fishTotal++;
			}
		}
	}
}

Player.prototype.changerTypeBet = function(bet){
	bet = bet>>0;
	if (bet >= 0 && bet <= 5) {
		this.updateTypeBet(bet);
		this.room.sendToAll({other:{updateType:{type:bet, map:this.map}}}, this);
	}
}

Player.prototype.bullet = function(bullet){
	if(this.money >= this.bet){
		let id = bullet.id>>0;
		this.money -= this.bet;
		this.meBulllet[id] = {type:this.typeBet, bet:this.bet};
		if (void 0 !== bullet.f) {
			this.room.sendToAll({other:{bulllet:{money:this.money, map:this.map, f:bullet.f}}}, this);
		}else{
			let x = bullet.x>>0;
			let y = bullet.y>>0;
			this.room.sendToAll({other:{bulllet:{money:this.money, map:this.map, x:x, y:y}}}, this);
		}
		this.isPlay = true;
	}else{
		this.client.red({me:{money:this.money}});
	}
}

Player.prototype.updateTypeBet = function(bet = null){
	if (bet !== null) {
		this.bet = this.room.root.bet[this.game][bet];
		this.typeBet = bet;
	}else{
		this.bet = this.room.root.bet[this.game][this.typeBet];
	}
}
Player.prototype.addRoom = function(room){
	this.room = room;
	return void 0;
}

Player.prototype.outGame = function(){
	// Thoát game sẽ trả lại tiền vào tài khoản và thoát game

	this.client.fish = null;
	this.client      = null;
	this.meBulllet   = null;

	if (!!this.room) {
		this.room.outRoom(this);
		this.room = null;
	}
	let win = this.money-this.moneyTotal;
	if (this.isPlay) {
		History.create({'uid':this.uid, 'room':this.game, 'money':win, 'fish':this.fishTotal, 'time':new Date()});
	}
	if (this.money > 0) {
		let uInfo = {red:this.money, totall:win};
		let Uid = this.uid;
		let self = this;
		UserInfo.updateOne({id:Uid}, {$inc:uInfo}, () =>{
			UserInfo.findOne({id:Uid}, (error,data) =>{
				console.log(Uid);
				console.log(data);
				if(void 0 !== redT.users[Uid]){
					redT.users[Uid].forEach(function(client){
						client.red({user:{red:data.red}});
					});
				}
				
			})
		})

	}
}

Player.prototype.lock = function(fish){
	if (void 0 !== this.room.fish[fish]) {
		this.room.sendToAll({lock:{f:fish, map:this.map}}, this);
	}
}

Player.prototype.unlock = function(){
	this.room.sendToAll({unlock:this.map}, this);
}

Player.prototype.nap = function(nap){
	nap = nap>>0;
	let bet = {
		1:100,
		2:500,
		3:1000,
	};
	let min = bet[this.game]*500;
	let max = bet[this.game]*5000;
	if (nap < min || nap > max) {
		this.client.red({notice:{title:'THẤT BẠI', text:'Số Dư Không Đủ...', load: false}});
	}else{
		UserInfo.findOne({id:this.uid}, 'red', function(err, user){
			if (!!user && user.red >= nap) {
				user.red -= nap;
				user.save();
				this.moneyTotal += nap;  // Tổng tiền mang vào
				this.money      += nap;  // số tiền chơi
				this.room.sendToAll({other:{map:this.map, money:this.money}}, this);
				this.client.red({me:{money:this.money, nap:true}});
			}else{
				this.client.red({notice:{title:'THẤT BẠI', text:'Số dư không khả dụng...', load: false}});
			}
		}.bind(this));
	}
}

Player.prototype.getScene = function(data){
	if (data.f && data.b) {
		let player = this.room.player[data.g-1];
		if (player && player.player && player.player.client) {
			if (player.player.scene === false) {
				player.player.client.red({scene:{f:data.f, b:data.b}});
			}
			player.player.scene = true;
		}
	}
}

module.exports = Player;
