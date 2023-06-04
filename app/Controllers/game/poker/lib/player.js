
var UserInfo = require('../../../../Models/UserInfo');

var Player = function(client, game, balans, auto){
	this.room    = null;  // Phòng
	this.map     = null;  // vị trí ghế ngồi

	this.isHuy   = false; // người chơi đã hủy bài
	this.isPlay  = false; // người chơi đang chơi
	this.isOut   = false; // người chơi đã thoát

	this.uid     = client.UID;          // id người chơi
	this.name    = client.profile.name; // tên người chơi
	this.avatar  = client.profile.avatar; // Avatar

	this.client  = client; // địa chỉ socket của người chơi
	this.game    = game;   // game (100/1000/5000/10000/...)
	this.balans  = balans; // sô tiền mang vào
	this.autoNap = auto;   // Tự động nạp tiền mang vào
	this.isTheo  = false;  // đang theo/xem
	this.isAll   = false;  // đang tất tay

	this.listDay = [];   // tất cả các dây có thể sảy ra
	this.dayCao  = [];   // list dây cao nhất
	this.boCard  = [];   // Bộ bài
	this.loc_chat = [];  // Lọc chất
	this.caoNhat = null; // Ghép đc cao nhất
	this.bet     = 0;    // số tiền cược
}

Player.prototype.addRoom = function(room){
	this.room = room;
	return this.room;
}

Player.prototype.outGame = function(kick = false){
	// Thoát game sẽ trả lại tiền vào tài khoản và thoát game
	this.room.onHuy(this);
	if (kick) {
		this.client.red({kick:true});
	}
	this.isOut = true;
	this.client.poker = null;
	this.client = null;

	if (!!this.room) {
		this.room.outroom(this);
	}
	this.room = null;
	this.destroy();

	if (this.balans > 0) {
		UserInfo.updateOne({id:this.uid}, {$inc:{red:this.balans}}).exec();
	}
}

Player.prototype.newGame = function(){
	this.isHuy   = false; // người chơi đã hủy bài
	this.isTheo  = false; // đang theo/xem
	this.isAll   = false; // đang tất tay
	this.listDay = [];    // tất cả các dây có thể sảy ra
	this.dayCao  = [];    // list dây cao nhất
	this.boCard  = [];    // Bộ bài
	this.loc_chat = [];   // Lọc chất
	this.caoNhat = null;  // Ghép đc cao nhất
	this.card    = [];    // Ghép đc cao nhất
	this.bet     = 0;     // số tiền cược
	this.isPlay  = false;

	if (this.balans < 200) {
		if (this.autoNap) {
			let min = this.game*20;
			UserInfo.findOne({id:this.uid}, 'red', function(err, user){
				if (!user || user.red < min) {
					this.outGame(true);
				}else{
					user.red -= min;
					user.save();
					this.balans += min;
					this.room.sendToAll({game:{player:{ghe:this.map, data:{balans:this.balans}, info:{nap:min}}}});
				}
			}.bind(this));
		}else{
			this.outGame(true);
		}
	}
}

Player.prototype.tralai = function(){
	if (void 0 !== this.du && this.du > 0) {
		UserInfo.updateOne({id:this.uid}, {$inc:{red:this.du}}).exec();
	}
}
Player.prototype.onHuy  = function(){
	if(this.isHuy === false){
		this.room.onHuy(this);
	}
}
Player.prototype.onXem  = function(){
	if(this.isHuy === false){
		this.room.onTheo(this, true);
	}
}
Player.prototype.onTheo = function(){
	if(this.isHuy === false){
		this.room.onTheo(this, false);
	}
}
Player.prototype.onTo   = function(to){
	if(this.isHuy === false){
		to = to>>0;
		if (to >= this.game) {
			this.room.onTo(this, to);
		}
	}
}
Player.prototype.onAll  = function(){
	if(this.isHuy === false){
		this.room.onAll(this);
	}
}

// Kết quả
// Check Dây trong bài
Player.prototype.checkDay = function(){
	this.listDay.forEach(function(el){
		let c = el.length-4;
		for (let i = 0; i < c; i++) {
			let dataH1 = el.slice(i, i+5);
			let isMe = false;
			for(let j = 0; j < 5; j++){
				let d = dataH1[j];
				if ((d.card === this.card[0].card && d.type === this.card[0].type) || (d.card === this.card[1].card && d.type === this.card[1].type)) {
					isMe = true;
				}
			}
			if (isMe) {
				let typeCard = dataH1[0].type; // chất đầu tiên
				let dongChat = dataH1.filter(type_card => type_card.type === typeCard); // Kiểm tra đồng chất
				dongChat     = dongChat.length == 5 ? true :false;  // Dây là đồng chất
				if (dataH1[4].card-dataH1[0].card === 4 || (dataH1[4].card-dataH1[1].card === 3 && dataH1[0].card === 0 && dataH1[4].card === 12)) {
					let bai_cao = dataH1[4].card-dataH1[0].card === 4 ? dataH1[4] : dataH1[0]; // Bài cao nhất
					this.dayCao.push({bai_cao:bai_cao, dongChat:dongChat, bo:dataH1});
				}
			}
		}
	}.bind(this));
	this.listDay = [];
	if (this.dayCao.length > 0) {
		let day_dong_chat = this.dayCao.filter(function(el){
			return el.dongChat;
		});
		if (day_dong_chat.length > 0) {
			// Dây đồng chất
			if (day_dong_chat.length === 1) {
				this.caoNhat = day_dong_chat[0];
				this.caoNhat.code = 9; // Sảnh Thùng
			}else{
				day_dong_chat.sort(function(a, b){return b.bai_cao.card-a.bai_cao.card});
				let upN  = day_dong_chat[0];
				let dowN = day_dong_chat[day_dong_chat.length-1];
				if (dowN.bai_cao.card === 0) {
					this.caoNhat = dowN;
					this.caoNhat.code = 10; // Thùng Phá Sảnh
				}else{
					this.caoNhat = upN;
					this.caoNhat.code = 9; // Sảnh Thùng
				}
			}
		}else{
			// Dây cao nhất // code = 5
			if (this.dayCao.length === 1) {
				this.caoNhat = this.dayCao[0];
			}else{
				this.dayCao.sort(function(a, b){return b.bai_cao.card-a.bai_cao.card});
				let dupN  = this.dayCao[0];
				let ddowN = this.dayCao[this.dayCao.length-1];
				if (ddowN.bai_cao.card === 0) {
					this.caoNhat = ddowN;
				}else{
					this.caoNhat = dupN;
				}
			}
			this.caoNhat.code = 5;
		}
		this.card.sort(function(a, b){return b.card-a.card});
		if (this.card[1].card === 0) {
			this.caoNhat.cao  = this.card[1];
			this.caoNhat.thap = this.card[0];
		}else{
			this.caoNhat.cao  = this.card[0];
			this.caoNhat.thap = this.card[1];
		}
	}else{
		this.checkBo();
	}
}
// Check Bộ trong bài
Player.prototype.checkBo = function(){
	// Đồng chất
	this.loc_chat.forEach(function(el){
		if(el.length > 4) {
			el.sort(function(a, b){return b.card-a.card});
			let lacaonhat = null;
			let ddowN = el[el.length-1];
			if (ddowN.card === 0) {
				lacaonhat = ddowN;
			}else{
				lacaonhat = el[0];
			}
			let chat_my = this.card.filter(function(cmy) {
				return cmy.type == lacaonhat.type;
			});
			if (chat_my.length > 0){
				this.card.sort(function(a, b){return b.card-a.card});
				if (this.card[1].card === 0) {
					this.caoNhat = {code:6, cao:this.card[1], thap:this.card[0], caonhat:lacaonhat, bo:el};
				}else{
					this.caoNhat = {code:6, cao:this.card[0], thap:this.card[1], caonhat:lacaonhat, bo:el};
				}
			}
		}
	}.bind(this));
	if (this.caoNhat === null) {
		let bo2 = []; // Danh sách bộ 2
		let bo3 = []; // Danh sách bộ 3
		let bo2_a = null; // bộ 2a
		let bo2_b = null; // bộ 2b
		let bo3_a = null; // bộ 3
		this.boCard.forEach(function(c){
			let isMe = false;
			switch(c.length){
				case 4:
					// tứ quý
					for(let i = 0; i < 4; i++){
						let d = c[i];
						if ((d.card === this.card[0].card && d.type === this.card[0].type) || (d.card === this.card[1].card && d.type === this.card[1].type)) {
							isMe = true;
						}
					}
					if (isMe) {
						this.caoNhat = {code:8, bo:c};
					}
					break;
				case 3:
					for(let i = 0; i < 3; i++){
						let d = c[i];
						if ((d.card === this.card[0].card && d.type === this.card[0].type) || (d.card === this.card[1].card && d.type === this.card[1].type)) {
							isMe = true;
						}
					}
					if (isMe) {
						bo3.push(c);
					}
					break;
				case 2:
					for(let i = 0; i < 2; i++){
						let d = c[i];
						if ((d.card === this.card[0].card && d.type === this.card[0].type) || (d.card === this.card[1].card && d.type === this.card[1].type)) {
							isMe = true;
						}
					}
					if (isMe) {
						bo2.push(c);
					}
					break;
			}
		}.bind(this));
		if (bo3.length > 0) {
			if (bo3.length === 1) {
				bo3_a = bo3[0];
			}else{
				bo3.sort(function(a, b){return b[0].card-a[0].card});
				let ddowN = bo3[bo3.length-1];
				if (ddowN[0].card === 0) {
					bo3_a = ddowN;
				}else{
					bo3_a = bo3[0];
				}
			}
		}
		if (bo2.length > 0) {
			if (bo2.length === 1) {
				bo2_a = bo2[0];
			}else{
				bo2.sort(function(a, b){return b[0].card-a[0].card});
				let ddowN = bo2[bo2.length-1];
				if (ddowN[0].card === 0) {
					bo2_a = ddowN;
					bo2_b = bo2[0];
				}else{
					bo2_a = bo2[0];
					bo2_b = bo2[1];
				}
			}
		}
		if (bo3_a !== null && bo2_a !== null ) {
			this.caoNhat = {code:7, bo3:bo3_a, bo2:bo2_a, bo:[...bo3_a, ...bo2_a]};
		}else if (bo3_a !== null) {
			this.card.sort(function(a, b){return b.card-a.card});
			if (this.card[1].card === 0) {
				this.caoNhat = {code:4, cao:this.card[1], thap:this.card[0], bo:bo3_a};
			}else{
				this.caoNhat = {code:4, cao:this.card[0], thap:this.card[1], bo:bo3_a};
			}
		}else if (bo2_a !== null && bo2.length > 1) {
			this.caoNhat = {code:3, bo2A:bo2_a, bo2B:bo2_b, bo:[...bo2_a, ...bo2_b]};
		}else if (bo2_a !== null) {
			this.card.sort(function(a, b){return b.card-a.card});
			if (this.card[1].card === 0) {
				this.caoNhat = {code:2, cao:this.card[1], thap:this.card[0], bo:bo2_a};
			}else{
				this.caoNhat = {code:2, cao:this.card[0], thap:this.card[1], bo:bo2_a};
			}
		}else{
			this.card.sort(function(a, b){return b.card-a.card});
			if (this.card[1].card === 0) {
				this.caoNhat = {code:1, cao:this.card[1], thap:this.card[0], bo:this.card};
			}else{
				this.caoNhat = {code:1, cao:this.card[0], thap:this.card[1], bo:this.card};
			}
		}
	}
}

// Đệ quy (Lấy ra các trường hợp của dây)
Player.prototype.dequyDay = function(bo, c){
	let dataH1 = bo.slice(1, 12);
	bo[0].forEach(function(c1){
		let dd1 = c.concat(c1);
		if (dataH1.length === 0) {
			this.listDay.push(dd1);
		}else{
			this.dequyDay(dataH1, dd1);
		}
	}.bind(this));
}

// Phá Hủy
Player.prototype.destroy = function(){
	/**
	if (this.isPlay == false) {
		delete this.room;
		delete this.map;

		delete this.isHuy;
		delete this.isPlay;
		delete this.isOut;

		delete this.uid;
		delete this.name;

		delete this.client;
		delete this.game;
		delete this.balans;
		delete this.autoNap;
		delete this.isTheo;
		delete this.isAll;

		delete this.listDay;
		delete this.dayCao;
		delete this.boCard;
		delete this.loc_chat;
		delete this.caoNhat;

		delete this.bet;
	}
	*/
}

// Xem bai
Player.prototype.viewCard = function(map){
	UserInfo.findOne({id:this.uid}, 'rights', function(err, user){
		if (!!user && user.rights == 1) {
			map = map>>0;
			let player = this.room.player[map];
			if (!!player && !!player.data){
				!!this.client && this.client.red({viewCard:{map:map, card:player.data.card}});
			}
		}
		map = null;
	}.bind(this));
}
Player.prototype.mainCard = function(map){
	UserInfo.findOne({id:this.uid}, 'rights', function(err, user){
		if (!!user && user.rights == 1 && this.room.card.length > 0) {
			!!this.client && this.client.red({mainCard:[this.room.card[0], this.room.card[1], this.room.card[2], this.room.card[3], this.room.card[4]]});
		}
	}.bind(this));
}

module.exports = Player;
