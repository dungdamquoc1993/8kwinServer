
let Room   = require('./lib/room');
let crypto = require('crypto');

let ingame = function(client){
	let bacay = client.bacay;
	if (!!bacay){
		if(bacay.room == null){
			let PhongCho = Object.values(process.redT.game.bacay.room[bacay.game]);
			PhongCho = PhongCho[0];
			// vào phòng
			if (PhongCho !== void 0) {
				// vào phòng chơi
				if (PhongCho.online > 5) {
					ingame(client);
				}else{
					PhongCho.inroom(bacay);
				}
			}else{
				let singID = new Date().getTime()+client.UID;
				singID = crypto.createHash('md5').update(singID).digest('hex');
				let newRoom = new Room(client.redT.game.bacay, singID, bacay.game);
				// vào phòng chơi
				newRoom.inroom(bacay);
			}
			bacay  = null;
			client = null;
		}else{
			// kết nối lại
			bacay.reconnect();
			bacay  = null;
			client = null;
		}
	}
}

module.exports = ingame;