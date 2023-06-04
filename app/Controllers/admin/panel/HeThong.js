
let get_data = require('./HeThong/get_data');
let TXBot    = require('./HeThong/TXBot');
let BCBot    = require('./HeThong/BCBot');
let clear    = require('./HeThong/clear');

let fanpage  = require('./HeThong/fanpage');

let SendAllTele  = require('./HeThong/tele');
let SendAllHomThu  = require('./HeThong/allhomthu');
let SendHomThu  = require('./HeThong/homthu');

module.exports = function(client, data) {
	if (!!data) {
		if (void 0 !== data.txbot) {
			TXBot(client, data.txbot);
		}
		if (void 0 !== data.bcbot) {
			BCBot(client, data.bcbot);
		}
		if (!!data.get_data){
			get_data(client);
		}
		if (!!data.clear){
			clear();
		}
		if (!!data.fanpage){
			fanpage(data.fanpage);
		}
		if (!!data.SendAllTele){
			SendAllTele(client, data.SendAllTele);
		}
		if (!!data.SendAllHomThu){
			SendAllHomThu(client, data.SendAllHomThu);
		}
		if (!!data.SendHomThu){
			SendHomThu(client, data.SendHomThu);
		}
	}
}
