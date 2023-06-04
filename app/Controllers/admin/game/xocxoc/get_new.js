
let fs = require('fs');
var path     = require('path');
let Helpers      = require('../../../../Helpers/Helpers');

module.exports = function(client) {

let txtJson = Helpers.getData('xocxoc');
	if(!!txtJson){
			client.red({xocxoc:{dices:[txtJson.red1, txtJson.red2, txtJson.red3, txtJson.red4], time_remain: client.redT.game.xocxoc.time, ingame: client.redT.game.xocxoc.ingame, info: client.redT.game.xocxoc.dataAdmin}});			
	}else{
		client.red({notice:{title:'THẤT BẠI', text:'Lỗi bất ngờ...'}});
	}
}
