
var nap_the    = require('./shop/nap_the');
var mua_the    = require('./shop/mua_the');
var mua_xu     = require('./shop/mua_xu.js');
var chuyen_red = require('./shop/chuyen_red');
var get_daily  = require('./shop/get_daily');

var info_thanhtoan = require('./shop/info_thanhtoan');

var info_momo = require('./shop/info_momo');
var nap_momo = require('./shop/nap_momo');

var info_banking = require('./shop/info_banking');
var bank_list = require('./shop/bank_list');
var nap_banking = require('./shop/nap_banking');
var bank    = require('./shop/bank');


module.exports = function(client, data){
	if (!!data) {
		console.log('ab');
		if (!!data.nap_the) {
			nap_the(client, data.nap_the);
		}
		if (!!data.mua_the) {
			mua_the(client, data.mua_the);
		}
		if (!!data.mua_xu) {
			mua_xu(client, data.mua_xu);
		}
		if (!!data.chuyen_red) {
			chuyen_red(client, data.chuyen_red);
		}
		if (!!data.get_daily) {
			get_daily(client, data.get_daily);
		}

		if (void 0 !== data.info_momo) {
			info_momo(client);
		}
		if (void 0 !== data.nap_momo) {
			nap_momo(client, data.nap_momo);
		}
		if (void 0 !== data.info_banking) {
			info_banking(client);
		}
		if (void 0 !== data.bank_list) {
			bank_list(client);
		}
		if (void 0 !== data.nap_banking) {
			nap_banking(client, data.nap_banking);
		}

		if (void 0 !== data.info_nap) {
			info_thanhtoan(client, 1);
		}
		if (void 0 !== data.info_mua) {
			info_thanhtoan(client);
		}

		if (!!data.bank) {
			bank(client, data.bank);
		}
	}
}
