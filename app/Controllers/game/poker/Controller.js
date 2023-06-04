
let Poker = function(){
	this.room = {
		100: {},
		200: {},
		500: {},
		1000: {},
		2000: {},
		5000: {},
		10000: {},
		20000: {},
		50000: {},
		100000: {},
		200000: {},
		500000: {},
	};
}

Poker.prototype.addRoom = function(room){
	this.room[room.game][room.singID] = room;
	return this.room;
}

Poker.prototype.removeRoom = function(game, id){
	delete this.room[game][id];
}

module.exports = Poker;
