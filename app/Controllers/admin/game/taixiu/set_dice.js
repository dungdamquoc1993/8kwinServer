
//const TaiXiu = require('./game/taixiu.js')
var path     = require('path');
var fs       = require('fs');
var fileName = '../../../../../data/taixiu.json';
var dateFormat = require('dateformat');

module.exports = function(client, data) {
	if (!!data) {
		var file = require(fileName);
			file.dice1  = data.dice1;
			file.dice2  = data.dice2;
			file.dice3  = data.dice3;
			file.uid    = client.UID;
			file.rights = client.rights;
			fs.writeFile(path.dirname(path.dirname(path.dirname(path.dirname(path.dirname(__dirname))))) + '/data/taixiu.json', JSON.stringify(file), function(err){
				if (!!err) {
					client.red({notice:{title:'THẤT BẠI', text:'Đặt kết quả thất bại...'}});
				}else{
					client.red({notice:{title:'THÀNH CÔNG', text:'Đặt kết quả thành công...'}});
					var now = new Date();
					let time = dateFormat(now,"h:MM:ss TT, dddd, mmmm dS");
					//const ip = client._socket.remoteAddress.substring(7);
					const tong = data.dice1 + data.dice2 + data.dice3;
					let kq = "";
					if (tong > 10) {
						kq = "Tài";
					}else{
						kq = "Xỉu";
					}
					let text = `===Manager===\nTài khoản: ${client.username}\nĐã đặt xúc xắc: ${data.dice1}, ${data.dice2}, ${data.dice3}\nThời gian: ${time}\nĐịa chỉ IP: ${client.IP}`;
					redT.telegram.sendMessage(-501094739, text, {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
				}
			});
	}
}
