
let RongHo_setDice = require('./rongho/set_dice');
let RongHo_getNew  = require('./rongho/get_new');

module.exports = function(client, data) {
	if (void 0 !== data.view) {
		client.gameEvent.viewRongHo = !!data.view;
	}
	if (void 0 !== data.get_new) {
		RongHo_getNew(client);
	}
	if (void 0 !== data.set_dice) {
		RongHo_setDice(client, data.set_dice);
	}
}
