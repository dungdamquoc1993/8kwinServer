
let HU           = require('../../../Models/HU');

let LongLan_red  = require('../../../Models/LongLan/LongLan_red');
let LongLan_xu   = require('../../../Models/LongLan/LongLan_xu');
let LongLan_user = require('../../../Models/LongLan/LongLan_user');

let MegaJP_user  = require('../../../Models/MegaJP/MegaJP_user');
let MegaJP_nhan  = require('../../../Models/MegaJP/MegaJP_nhan');

let UserInfo  = require('../../../Models/UserInfo');
let Helpers   = require('../../../Helpers/Helpers');

module.exports = function(client, data){
	console.log(data);
	if (!!data) {
		let cash  = data.cash;                   // Cash
		if (cash > 0) {
			UserInfo.findOne({id:client.UID},'red name', function(err, user){
				if(user.red < cash) {
					client.red({shootfish:{status:0, notice:'Bạn không đủ Xu để đổi!'}});
				}else{
					UserInfo.updateOne({id: client.UID}, {$inc:{red:-cash}}).exec();
					client.red({shootfish:{status:1, notice:'Đổi Cash thành công!', user:{red:user.red-cash}}});
				}
			});
		}
		else{
			UserInfo.findOne({id:client.UID},'red name', function(err, user){
				UserInfo.updateOne({id: client.UID}, {$inc:{red:-cash}}).exec();
				client.red({shootfish:{status:1, notice:'Đổi Xu thành công!', user:{red:user.red-cash}}});
			});
		}
	}
};
