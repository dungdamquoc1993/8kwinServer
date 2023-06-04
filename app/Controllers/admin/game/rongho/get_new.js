
let fs = require('fs');
var path     = require('path');
let Helpers      = require('../../../../Helpers/Helpers');

module.exports = function(client) {
	let txtJson = Helpers.getData('rongho');
	if(!!txtJson){
		client.red({rongho:{dices:[txtJson.rong, txtJson.ho, txtJson.chatrong, txtJson.chatho], time_remain: client.redT.rongho.time, ingame: client.redT.rongho.ingame, info: client.redT.rongho.dataAdmin}});
		}else{
		client.red({notice:{title:'THẤT BẠI', text:'Lỗi bất ngờ...'}});
	}
	
}
