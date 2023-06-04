
let first  = require('./Controllers/User.js').first;
let onPost = require('./Controllers/onPost.js');

let auth = function(client) {
	client.gameEvent = {};
	client.scene = 'home';
	first(client);
	client = null;
}

let signMethod = function(client) {
	client.TTClear = function(){
		if (!!this.caothap) {
			clearTimeout(this.caothap.time);
			this.caothap.time = null;
			this.caothap = null;
		}

		if (!!this.poker) {
			this.poker.outGame();
			this.poker = null;
		}
		if (!!this.bacay) {
			this.bacay.disconnect();
			this.bacay = null;
		}

		if (!!this.fish) {
			this.fish.outGame();
		}
		if (this.redT) {
			let xocxoc = this.redT.game.xocxoc;
			let rongho = this.redT.rongho;
			if (xocxoc.clients[this.UID] === this) {
				delete xocxoc.clients[this.UID];
				let clients = Object.keys(xocxoc.clients).length;
				Object.values(xocxoc.clients).forEach(function(users){
					if (client !== users) {
						users.red({xocxoc:{ingame:{client:clients}}});
					}
				});
			}
			if (rongho.clients[this.UID] === this) {
				delete rongho.clients[this.UID];
				let clients = Object.keys(rongho.clients).length;
				Object.values(rongho.clients).forEach(function(users){
					if (client !== users) {
						users.red({rongho:{ingame:{client:clients}}});
					}
				});
			}
			xocxoc = null;
			rongho = null;
			delete this.redT;
		}

		this.TTClear = null;
	}
	client = null;
}

module.exports = {
	auth:       auth,
	message:    onPost,
	signMethod: signMethod,
};
