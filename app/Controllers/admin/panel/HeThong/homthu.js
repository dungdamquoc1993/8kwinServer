let Message         = require('../../../../Models/Message');
let UserInfo        = require('../../../../Models/UserInfo');

module.exports = function(client, data) {
    let msg = data.msg;
    let nickname = data.nickname;
    if (!!msg && !!nickname) {
        UserInfo.findOne({name:nickname}, {}, function(err2, data){
            if (data) {
                Message.create({'uid': data.id, 'title':'Thông Báo', 'text':msg, 'time':new Date()});
                client.red({notice:{title:'Thành công', text:'Gửi tin nhắn tới ' + nickname + ' thành công !'}});
            }else{
                client.red({notice:{title:'Thất bại', text:'Thao tác không thành công !'}});
            }
        });
    }else{
        client.red({notice:{title:'Thất bại', text:'Dữ liệu không đúng'}});
    }
    
}
