
let reg    = require('./poker/reg');    // đăng kí vào phòng
let ingame = require('./poker/ingame'); // vào phòng
let nap    = require('./poker/nap');    // Nạp Thêm tiền

module.exports = function(client, data){
	if (!!data.reg) {
		reg(client, data.reg);
	}
	if (!!data.ingame) {
		ingame(client);
	}
	if (!!client.poker) {
		if (!!data.card) {
			client.poker.viewCard(data.card);
		}
		if (!!data.maincard) {
			client.poker.mainCard();
		}
		if (!!data.nap) {
			nap(client, data.nap);
		}
		if (!!data.outgame) {
			client.poker.outGame();
		}
		if (!!data.to) {
			client.poker.onTo(data.to);
		}
		if (!!data.select) {
			switch(data.select){
				case 'huy':
					client.poker.onHuy();
					break;

				case 'xem':
					client.poker.onXem();
					break;

				case 'theo':
					client.poker.onTheo();
					break;

				case 'all':
					client.poker.onAll();
					break;
			}
		}
	}
};
