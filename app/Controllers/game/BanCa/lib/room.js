
var Room = function(root, id, room){
	this.root = root; // Root
	this.id   = id;   // ID phòng
	this.room = room; // loại phòng
///**
	this.timeWait = setTimeout(function(){
		//clearTimeout(this.timeWait);
		//this.root.removeWait(this.room, this.id);
		this.playGame();
	}.bind(this), 2000);  // thời gian chờ phòng
//*/
	this.timeFish = null; // thời gian ra cá
	this.timeGame = 0;    // thời gian Game


	this.fish = {};       // Cá
	this.fishID = 0;      // id

	// Những người chơi
	this.player = [
		{id:1, player:null},
		{id:2, player:null},
		{id:3, player:null},
		{id:4, player:null},
	]; // Những người chơi
}

Room.prototype.gameStart = function(){
	let fishs = [];
	let id    = null;
	let fish  = null;
	let fishU = null;
	let rand  = null;
	let f     = null;
	let data  = null;
	for (let i = 0; i < 6; i++) {
		id = this.fishID++;
		fish = ((Math.random()*17)>>0)+1;
		fishU = this.root.fish[fish];
		rand = (Math.random()*fishU.clip)>>0;
		f = {f:fish, coll:{0:this.collision(fishU), 1:this.collision(fishU), 2:this.collision(fishU), 3:this.collision(fishU), 4:this.collision(fishU), 5:this.collision(fishU)}};
		this.fish[id] = f;
		data = {id:id, f:fish, r:rand};
		fishs.push(data);
	}

	for (let i = 0; i < 6; i++) {
		id = this.fishID++;
		fish = ((Math.random()*10)>>0)+1;
		fishU = this.root.fish[fish];
		rand = (Math.random()*fishU.clip)>>0;
		f = {f:fish, coll:{0:this.collision(fishU), 1:this.collision(fishU), 2:this.collision(fishU), 3:this.collision(fishU), 4:this.collision(fishU), 5:this.collision(fishU)}};
		this.fish[id] = f;
		data = {id:id, f:fish, r:rand};
		fishs.push(data);
	}

	let all = [];
	let kkT = Math.floor(Math.random()*(9-5+1))+5;
	fish = ((Math.random()*3)>>0)+1;
	fishU = this.root.fish[fish];
	rand = (Math.random()*fishU.clip)>>0;
	for (let i = 0; i < kkT; i++) {
		id = this.fishID++;
		f = {f:fish, coll:{0:this.collision(fishU), 1:this.collision(fishU), 2:this.collision(fishU), 3:this.collision(fishU), 4:this.collision(fishU), 5:this.collision(fishU)}};
		this.fish[id] = f;
		data = {id:id, f:fish, r:rand};
		all.push(data);
	}
	fishs.push({t:0.8, f:all});
	this.sendToAll({fishs:{fs:fishs}});
	this.timeGame++;

	all   = null;
	kkT   = null;
	fishs = null;
	id    = null;
	fish  = null;
	fishU = null;
	rand  = null;
	f     = null;
	data  = null;
}

Room.prototype.addFish = function(){
	if (this.timeGame > 350) { //650
		this.playRound();
		return void 0;
	}
	let fishs = [];
	let id    = null;
	let fish  = null;
	let fishU = null;
	let rand  = null;
	let f     = null;
	let data  = null;

	if (!(this.timeGame%26)) {
		id = this.fishID++;
		fish = Math.floor(Math.random()*(27 - 19 + 1)) + 19;
		fishU = this.root.fish[fish];
		rand = (Math.random()*fishU.clip)>>0;
		f = {f:fish, coll:{0:this.collision(fishU), 1:this.collision(fishU), 2:this.collision(fishU), 3:this.collision(fishU), 4:this.collision(fishU), 5:this.collision(fishU)}};
		this.fish[id] = f;
		data = {id:id, f:fish, r:rand};
		fishs.push(data);
	}
	if (!(this.timeGame%16)) {
		for (let i = 0; i < 5; i++) {
			id = this.fishID++;
			fish = Math.floor(Math.random()*(18 - 7 + 1)) + 7;
			fishU = this.root.fish[fish];
			rand = (Math.random()*fishU.clip)>>0;
			f = {f:fish, coll:{0:this.collision(fishU), 1:this.collision(fishU), 2:this.collision(fishU), 3:this.collision(fishU), 4:this.collision(fishU), 5:this.collision(fishU)}};
			this.fish[id] = f;
			data = {id:id, f:fish, r:rand};
			fishs.push(data);
		}
		let all = [];
		let kkT = Math.floor(Math.random()*(8-4+1))+4;
		fish = ((Math.random()*3)>>0)+1;
		fishU = this.root.fish[fish];
		rand = (Math.random()*fishU.clip)>>0;
		for (let i = 0; i < kkT; i++) {
			id = this.fishID++;
			f = {f:fish, coll:{0:this.collision(fishU), 1:this.collision(fishU), 2:this.collision(fishU), 3:this.collision(fishU), 4:this.collision(fishU), 5:this.collision(fishU)}};
			this.fish[id] = f;
			data = {id:id, f:fish, r:rand};
			all.push(data);
		}
		fishs.push({t:0.8, f:all});
		all = null;
		kkT = null;
	}

	if (!(this.timeGame%10)) {
		for (let i = 0; i < 5; i++) {
			id = this.fishID++;
			fish = ((Math.random()*6)>>0)+1;
			fishU = this.root.fish[fish];
			rand = (Math.random()*fishU.clip)>>0;
			f = {f:fish, coll:{0:this.collision(fishU), 1:this.collision(fishU), 2:this.collision(fishU), 3:this.collision(fishU), 4:this.collision(fishU), 5:this.collision(fishU)}};
			this.fish[id] = f;
			data = {id:id, f:fish, r:rand};
			fishs.push(data);
		}
	}

	if (!(this.timeGame%3)) {
		for (let i = 0; i < 3; i++) {
			id = this.fishID++;
			fish = ((Math.random()*6)>>0)+1;
			fishU = this.root.fish[fish];
			rand = (Math.random()*fishU.clip)>>0;
			f = {f:fish, coll:{0:this.collision(fishU), 1:this.collision(fishU), 2:this.collision(fishU), 3:this.collision(fishU), 4:this.collision(fishU), 5:this.collision(fishU)}};
			this.fish[id] = f;
			data = {id:id, f:fish, r:rand};
			fishs.push(data);
		}
	}

	fishs.length > 0 && this.sendToAll({fishs:{fs:fishs}});
	this.timeGame++;

	fishs = null;
	id    = null;
	fish  = null;
	fishU = null;
	rand  = null;
	f     = null;
	data  = null;
}

Room.prototype.playGame = function(){
	this.gameStart();
	this.timeFish = setInterval(this.addFish.bind(this), 1000);
}

Room.prototype.resetGame = function() {
	clearInterval(this.timeFish);
	this.fish     = {};
	this.fishID   = 0;
	this.timeGame = 0;
}

Room.prototype.playRound = function() {
	this.resetGame();
	this.sendToAll({round:true});
	this.timeWait = setTimeout(function(){
		let idG = Math.floor(Math.random()*(22-20+1))+20;
		let fish = this.root.group[idG];
		let rand = (Math.random()*fish.clip)>>0;
		let time = fish.t;
		fish = this.groupData(fish, null, rand);
		this.sendToAll({fish:fish});

		this.timeWait = setTimeout(function(){
			this.resetGame();
			this.playGame();
		}.bind(this), time*1000);
	}.bind(this), 2000);
}

Room.prototype.groupData = function(data, a = null, r = null) {
	let g = {'g':data.g, 'z':data.z};
	if (a !== null) g.a = a;
	if (r !== null) g.r = r;
	g.f = data.f.map(function(fish){
		let id = this.fishID++;
		let f = {f:fish, coll:{0:this.collision(this.root.fish[fish]), 1:this.collision(this.root.fish[fish]), 2:this.collision(this.root.fish[fish]), 3:this.collision(this.root.fish[fish]), 4:this.collision(this.root.fish[fish]), 5:this.collision(this.root.fish[fish])}};
		this.fish[id] = f;
		return {id:id, f:fish};
	}.bind(this));
	return g;
}

Room.prototype.collision = function(data) {
	return Math.floor(Math.random()*(data.max-data.min+1))+data.min;
}

Room.prototype.sendToAll = function(data, player = null){
	this.player.forEach(function(ghe){
		if (!!ghe.player && ghe.player !== player) {
			!!ghe.player.client && ghe.player.client.red(data);
		}
	});
}

Room.prototype.inRoom = function(player){
	let gheTrong = this.player.filter(function(t){return t.player === null}); // lấy các ghế trống
	let rand = (Math.random()*gheTrong.length)>>0;
	let Down = gheTrong[rand];
	//gheTrong = gheTrong[0];
	this.player[Down.id-1].player = player; // ngồi
	player.map  = Down.id;                  // vị trí ngồi
	player.room = this;
	player.updateTypeBet();
	this.sendToAll({ingame:{ghe:player.map, data:{name:player.client.profile.name, balans:player.money, typeBet:player.typeBet}}}, player);

	let getInfo = this.player.map(function(ghe){
		if (!!ghe.player) {
			return {ghe:ghe.id, data:{name:ghe.player.client.profile.name, balans:ghe.player.money, typeBet:ghe.player.typeBet}};
		}else{
			return {ghe:ghe.id, data:null};
		}
	});
	let client = {infoGhe:getInfo, meMap:player.map};
	player.client.red(client);

	if (gheTrong.length === 1) {
		//clearTimeout(this.timeWait);
		this.root.removeWait(this.room, this.id);
		//this.playGame();
	}else{
		this.root.addWait(this.room, this);
	}
}

Room.prototype.outRoom = function(player){
	this.player[player.map-1].player = null;
	let gheTrong = this.player.filter(function(t){return t.player === null}); // lấy các ghế trống
	if (gheTrong.length === 4) {
		clearInterval(this.timeFish);
		clearTimeout(this.timeWait);
		this.root.removeWait(this.room, this.id);
		this.player.forEach(function(ghe){
			ghe = null;
		});
		// xóa phòng
		delete this.player;
		delete this.root;
	}else{
		this.sendToAll({outgame:player.map});
	}
}

module.exports = Room;
