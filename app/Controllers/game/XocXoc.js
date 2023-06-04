
let ingame  = require('./XocXoc/ingame');
let outgame = require('./XocXoc/outgame');
let cuoc    = require('./XocXoc/cuoc');
let history = require('./XocXoc/history');
var tops     = require('./XocXoc/tops');

module.exports = function(client, data){
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
