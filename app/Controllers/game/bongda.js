
var cuoc = require('./bongda/cuoc');
var info = require('./bongda/info');
var getphien = require('./bongda/get_phien');
var log  = require('./bongda/log');
var top  = require('./bongda/top');

module.exports = function(client, data){
	if (!!data.info) {
		info(client, data.info)
	}
	if (!!data.getphien) {
		getphien(client, data.getphien)
	}
	if (!!data.cuoc) {
		console.log(data.cuoc);
		cuoc(client, data.cuoc)
	}
	if (!!data.log) {
		log(client, data.log)
	}
	if (void 0 !== data.top) {
		top(client, data.top)
	}
};
