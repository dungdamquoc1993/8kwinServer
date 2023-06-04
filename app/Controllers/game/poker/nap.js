
let UserInfo = require('../../../Models/UserInfo');
let Helpers  = require('../../../Helpers/Helpers');

module.exports = function(client, data){
	if (!!data.balans) {
		let balans = data.balans>>0;
		let auto   = !!data.auto;
		let room   = client.poker.game;
		let min = room*20;
		let max = room*200;
		if (balans < min || balans > max) {
			client.red({notice:{title:'THẤT BẠI', text:'Dữ liệu không đúng...', load:false}});
		}else{
			let totall = client.poker.balans+balans;
			if(totall > max){
				client.red({notice:{title:'THẤT BẠI', text:'Phòng chơi chỉ cho phép mang tối đa ' + Helpers.numberWithCommas(max) + ' Tiền.!!', load:false}});
			}else{
				UserInfo.findOne({id:client.UID}, 'red', function(err, user){
					if (!user || user.red < min) {
						client.red({notice:{title:'THẤT BẠI', text:'Bạn cần tối thiểu ' + min + ' Tiền để vào phòng.!!', load:false}});
					}else{
						if (user.red < balans) {
							client.red({notice:{title:'THẤT BẠI', text:'Tài khoản không đủ để Nạp Poker.!!', load:false}});
						}else{
							user.red -= balans;
							user.save();
							client.poker.balans += balans;
							client.poker.autoNap = auto;
							client.poker.room.sendToAll({game:{player:{ghe:client.poker.map, data:{balans:client.poker.balans}, info:{nap:balans}}}}, client.poker);
							client.red({load:false, nap:false, game:{player:{ghe:client.poker.map, data:{balans:client.poker.balans}, info:{nap:balans}}}});
						}
					}
				});
			}
		}
	}
};
