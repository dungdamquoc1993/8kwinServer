
let reg     = require('./reg');    // đăng kí vào phòng
let history = require('./history');    // đăng kí vào phòng

module.exports = function(client, data){
	if (!!data.collision) {
		client.fish.collision(data.collision);
	}
	if (void 0 !== data.bullet) {
		client.fish.bullet(data.bullet);
	}
	if (!!data.reg) {
		reg(client, data.reg);
	}
	if (!!data.outgame && !!client.fish) {
		client.fish.outGame();
	}
	if (void 0 !== data.typeBet) {
		client.fish.changerTypeBet(data.typeBet);
	}
	if (void 0 !== data.lock) {
		client.fish.lock(data.lock);
	}
	if (void 0 !== data.unlock) {
		client.fish.unlock();
	}
	if (!!data.nap) {
		client.fish.nap(data.nap);
	}
	if (!!data.log) {
		history(client, data.log);
	}
	if (void 0 !== data.getScene) {
		client.fish.getScene(data.getScene);
	}
};
