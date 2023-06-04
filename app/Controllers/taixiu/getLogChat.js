
var TXChat  = require('../../Models/TaiXiu_chat');
var UserInfo = require('../../Models/UserInfo');

module.exports = function(client){
	var data;
	TXChat.find({},'name value', {sort:{'_id':-1}, limit: 1}, function(err, post) {
		if (post.length){
			Promise.all(post.map(function(obj){
			UserInfo.findOne({name:obj.name}, 'red lastVip redPlay vip', function(err, user){
				/* if(user.redPlay != null ){
					var vipHT = ((user.redPlay-user.lastVip)/1000000)>>0;
				}else{ */
					var vipHT =1;
				//}
				
				var vipLevel = 1;
				if (vipHT >= 120000) {
					vipLevel = 9;
				}else if (vipHT >= 50000){
					vipLevel = 8;
				}else if (vipHT >= 15000){
					vipLevel = 7;
				}else if (vipHT >= 6000){
					vipLevel = 6;
				}else if (vipHT >= 3000){
					vipLevel = 5;
				}else if (vipHT >= 1000){
					vipLevel = 4;
				}else if (vipHT >= 500){
					vipLevel = 3;
				}else if (vipHT >= 100){
					vipLevel = 2;
				}
		data = [{'user':obj.name, 'value':obj.value, 'vip': vipLevel}]

		client.red({taixiu:{chat:{logs: data}}});
		
	})
	}))
		}
	});
}
