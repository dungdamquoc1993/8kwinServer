
let ingame  = require('./RongHo/ingame');
let outgame = require('./RongHo/outgame');
let cuoc    = require('./RongHo/cuoc');
let history = require('./RongHo/history');
let tops     = require('./RongHo/tops');

module.exports = function(client, data){

	console.log(data);
	if (!!data.ingame) {
		ingame(client);
	}
	if (!!data.outgame) {
		outgame(client);
	}
	if (!!data.cuoc) {
		cuoc(client, data.cuoc);
	}
	if (!!data.log) {
		history(client, data.log);
	}
	if (!!data.tops) {
		tops(client, data.tops)
	}
	client = null;
	data   = null;
};
