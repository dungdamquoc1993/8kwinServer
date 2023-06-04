
let Room   = require('./lib/room');
let crypto = require('crypto');

let ingame = function(client){
	let poker = client.poker;
	if (!!poker && poker.room == null) {
		let PhongCho = Object.values(client.redT.game.poker.room[poker.game]);
		PhongCho = PhongCho[0];
		// vào phòng
		if (PhongCho !== void 0) {
			// vào phòng chơi
			if (PhongCho.online > 5) {
				ingame(client);
			}else{
				PhongCho.inroom(poker);
			}
		}else{
			let singID = new Date().getTime()+client.UID;
			singID = crypto.createHash('md5').update(singID).digest('hex');
			let newRoom = new Room(client.redT.game.poker, singID, poker.game);
			// vào phòng chơi
			newRoom.inroom(poker);
		}
	}
	poker  = null;
	client = null;
}

module.exports = ingame;