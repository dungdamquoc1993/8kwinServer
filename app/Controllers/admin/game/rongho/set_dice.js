
let fs = require('fs');
var path     = require('path');

module.exports = function(client, data) {
	fs.readFile(path.dirname(path.dirname(path.dirname(path.dirname(path.dirname(__dirname))))) + '/data/rongho.json', 'utf8', (errcf, txtJson) => {
		try {
			txtJson = JSON.parse(txtJson);
			for (let [key, value] of Object.entries(data)) {
				console.log(key);
				//key = (key>>0)+1;
				if (key == 0) {
					txtJson['chatrong'] = value;
				}if (key == 1) {
					txtJson['chatho'] = value;
				}if (key == 3) {
					txtJson['rong'] = value;
				}if (key == 4) {
					txtJson['ho'] = value;
				}
				
			}
			fs.writeFile(path.dirname(path.dirname(path.dirname(path.dirname(path.dirname(__dirname))))) + '/data/rongho.json', JSON.stringify(txtJson), function(err){});
			client.redT.admins[client.UID].forEach(function(obj){
				obj.red({rongho:{dices:[txtJson.rong, txtJson.ho, txtJson.chatrong, txtJson.chatho]}});
			});
		} catch (error) {
			client.red({notice:{title:'THẤT BẠI', text:'Đặt kết quả thất bại...'}});
		}
	});
}
