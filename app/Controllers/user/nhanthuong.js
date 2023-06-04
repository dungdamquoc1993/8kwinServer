
const UserInfo = require('../../Models/UserInfo');
const helper   = require('../../Helpers/Helpers');

module.exports = function(client){
	UserInfo.findOne({id:client.UID}, 'red lastVip redPlay vip', function(err, user){
		var vipHT = ((user.redPlay-user.lastVip)/1000000)>>0; // Điểm vip Hiện Tại
		let score = user.redPlay/1000000>>0;
		var red   = 200; // Giá điểm vip
		var vipLevel = 1;
		var vipPre   = 0; // Điểm víp cấp Hiện tại
		var vipNext  = 100; // Điểm víp cấp tiếp theo
		 if (score >= 120000) {//vip 9
		 	red = 2700;
			 vipLevel = 9;
			 vipPre   = 120000;
			 vipNext  = 0;
		 }else if (score >= 50000){//vip 8
		 	red = 2565;
			 vipLevel = 8;
			 vipPre   = 50000;
			 vipNext  = 120000;
		 }else if (score >= 15000){//vip 7
		 	red = 2430;
			 vipLevel = 7;
			 vipPre   = 15000;
			 vipNext  = 50000;
		 }else if (score >= 6000){//vip 6
		 	red = 2160;
			 vipLevel = 6;
			 vipPre   = 6000;
			 vipNext  = 15000;
		 }else if (score >= 3000){//vip 5 
		 	red = 1890;
			 vipLevel = 5;
			 vipPre   = 3000;
			 vipNext  = 6000;
		 }else if (score >= 1000){ //vip 4 
		 	red = 1350;
			 vipLevel = 4;
			 vipPre   = 1000;
			 vipNext  = 3000;
		 }else if (score >= 500){ //vip 3
		 	red = 972;
			 vipLevel = 3;
			 vipPre   = 500;
			 vipNext  = 1000;
		 }else if (score >= 100){ //vip 2
		 	red = 405;
			 vipLevel = 2;
			 vipPre   = 100;
			 vipNext  = 500;
		 }
		var tien = vipHT*red; // Tiền thưởng lastVip
		if (tien > 0) {
			user.red     = user.red*1 + tien; // cập nhật red
			user.vip    += vipHT;             // vip tích lũy
			user.lastVip = user.redPlay;      // Nhận thưởng lần cuối
			user.save();
			client.red({profile:{level: {level: vipLevel, vipNext: vipNext, vipPre: vipPre, vipTL: user.vip+vipHT, vipHT: vipHT, score: score}}, notice:{text: `Bạn đã đổi ${vipHT} điểm ở mốc VIP ${vipLevel} được ${helper.numberWithCommas(tien)} XU`, title: 'THÀNH CÔNG'}, user:{red: user.red}});
		}else{
			client.red({notice:{text: 'Bạn chưa đủ cấp VIP để đổi thưởng...', title: 'THẤT BẠI'}});
		}
	});
}
