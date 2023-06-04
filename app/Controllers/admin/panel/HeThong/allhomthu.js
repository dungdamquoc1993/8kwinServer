let Message         = require('../../../../Models/Message');
let UserInfo        = require('../../../../Models/UserInfo');


module.exports = function(client, data) {
	if (!!data) {
		UserInfo.find({}, 'id', function(err, users){
			users.forEach(function(user){
				Message.create({'uid': user.id, 'title':'Thông Báo', 'text': data, 'time':new Date()});
				client.red({notice:{title:'Thành công', text:'Gửi tin nhắn tới tất cả thành công !'}});
			});
		});
	}else{
		client.red({notice:{title:'Thất bại', text:'Dữ liệu không đúng'}});
	}
    
}
