
const changecoin  = require('./shootfish/changecoin');
const bonus = require('./shootfish/bonus');
const log   = require('./shootfish/log');
const top   = require('./shootfish/top');

module.exports = function(client, data){
	if (!!data.bonus) {
		bonus(client, data.bonus)
	}
	if (!!data.changecoin) {
		//changecoin(client, data.changecoin)
	}
	if (!!data.log) {
		log(client, data.log)
	}
	if (void 0 !== data.top) {
		top(client, data.top)
	}
};
