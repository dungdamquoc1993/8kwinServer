
let reg    = require('./reg');    // đăng kí vào phòng
let ingame = require('./ingame'); // vào phòng

module.exports = function(client, data){
	if (!!data.reg) {
		reg(client, data.reg);
	}
	if (!!data.ingame) {
		ingame(client);
	}
	if (!!client.bacay) {
		if (!!data.viewcard) {
			client.bacay.viewCard(data.viewcard);
		}
		if (!!data.listCard) {
			client.bacay.listCard();
		}
		if (!!data.setCard) {
			client.bacay.setCard(data.setCard);
		}
		if (!!data.lat) {
			client.bacay.onLat();
		}
		if (!!data.cuocG) {
			client.bacay.cuocGa();
		}
		if (!!data.cuocC) {
			client.bacay.cuocChuong(data.cuocC);
		}
		if (!!data.regOut) {
			client.bacay.onRegOut();
		}

	}
	client = null;
	data = null;
};
