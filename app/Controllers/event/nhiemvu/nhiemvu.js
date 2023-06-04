var Bank_history = require('../../../Models/Bank/Bank_history');
var UserInfo = require('../../../Models/UserInfo');
var TX_User  = require('../../../Models/TaiXiu_user');
var nhiemvu  = require('../../../Models/NhiemVu');
let DataNhiemVu    = require('../../../../data/nhiemvu');

var Helper      = require('../../../Helpers/Helpers');

function taixiu(client, data){
	if(!!data){
		//console.log(data);
        TX_User.findOne({'uid': client.UID}, 'tRedPlay tWinRed tLostRed', function(err2, result){
			if (!!result) {
				let tRedPlay = result.tRedPlay;
				if (tRedPlay < data.dieukien) {
					client.red({notice:{title:"NHIỆM VỤ", text:'Chưa đủ điều kiện để nhận thưởng !'}});
				} else {
					nhiemvu.findOne({'uid':client.UID, 'id':data.id}, function(err1, crack){
						if (crack) {
							client.red({notice:{title:"NHIỆM VỤ", text:'Bạn đã nhận thưởng rồi !'}});
						}else{
							UserInfo.findOneAndUpdate({'id':client.UID}, {$inc:{red:data.phanthuong}}, function(err3, user) {
								if (user) {
									if (void 0 !== client.redT.users[client.UID]) {
										Promise.all(client.redT.users[client.UID].map(function(obj){
											obj.red({user:{red:user.red}});
										}));
									}
								}
								nhiemvu.create({'uid':client.UID, 'id':data.id, 'createdate':new Date()});
								client.red({notice:{title:"NHIỆM VỤ", text:'Chúc mừng bạn nhận được ' + Helper.numberWithCommas(data.phanthuong) + ' XU'}});
							});	
						}
					});
				}
			}else if (err2) {
				console.log(err2);
			}
        });
	}
}

module.exports = function(client, data) {
	if (!!data) {
		switch (data) {
			case data > 100 && data < 999:
				taixiu(client, data)
				break;
			default:
				taixiu(client, data)
				break;
		}

	}
};
