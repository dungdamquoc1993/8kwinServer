
let UserInfo = require('../../../Models/UserInfo');
let Helpers  = require('../../../Helpers/Helpers');
let Player   = require('./lib/player');
let Room     = require('./lib/room');
let crypto = require('crypto');

module.exports = function(client, data){
	if (!!data.room && !!data.balans) {
		let room   = data.room>>0;
		let balans = data.balans>>0;

		let bet = {
			1:100,
			2:500,
			3:1000,
		};

		if (room === 1 || room === 2 || room === 3){
			let min = bet[room]*500;
			let max = bet[room]*5000;
			if (balans < min || balans > max) {
				client.red({notice:{title:'THẤT BẠI', text:'Dữ liệu không đúng...', load: false}});
			}else{
				let inGame = false;
				client.redT.users[client.UID].forEach(function(obj){
					if(!!obj.fish){
						inGame = true;
					}
				});
				if (inGame) {
					client.red({notice:{title:'CẢNH BÁO', text:'Bạn hoặc ai đó đang chơi BẮN CÁ bằng tài khoản này ...', load: false}});
				}else{
					UserInfo.findOne({id:client.UID}, 'red', function(err, user){
						if (!user || user.red < min) {
							client.red({notice:{title:'THẤT BẠI', text:'Bạn cần tối thiểu ' + Helpers.numberWithCommas(min) + ' GOLD để vào phòng.!!', load: false}});
						}else{
							if(user.red<balans) 
							{
								client.red({notice:{title:'THẤT BẠI', text:'Bạn không đủ GOLD để vào phòng.!!', load: false}});
								return;
							}
							user.red -= balans;
							user.save();
							client.fish = new Player(client, room, balans);
							// Tìm phòng chờ
							let PhongCho = Object.values(client.redT.game.fish['wait'+room]);
							PhongCho = PhongCho[0];
							if (PhongCho !== void 0) {
								// có phòng chờ
								PhongCho.inRoom(client.fish);
							}else{
								// tạo phòng mới
								let singID = new Date().getTime() + client.UID;
								singID = crypto.createHash('md5').update(singID).digest('hex');
								let Game = new Room(client.redT.game.fish, singID, room);
								client.redT.game.fish['wait'+room][singID] = Game; // Thêm phòng chờ
								Game.inRoom(client.fish);
							}
						}
					})
				}
			}
		}else{
			client.red({notice:{title:'THẤT BẠI', text:'Dữ liệu không đúng...', load: false}});
		}
	}
}
