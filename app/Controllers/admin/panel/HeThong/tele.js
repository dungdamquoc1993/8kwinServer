let telegram = require('../../../../Models/Telegram');


module.exports = function(client, data) {
    if (!!data) {
        telegram.find({}, 'form', function(err, teles){
            teles.forEach(function(tele){
                redT.telegram.sendMessage(tele.form, data, {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
                client.red({notice:{title:'Thành công', text:'Gửi tin nhắn tới tất cả thành công !'}});
            });
        });
    }else{
        client.red({notice:{title:'Thất bại', text:'Dữ liệu không đúng'}});
    }
    
}
