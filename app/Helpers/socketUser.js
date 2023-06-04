
let Poker  = require('../Controllers/game/poker/Controller');
let BaCay  = require('../Controllers/game/BaCay/Controller');

let XocXoc = require('../Controllers/game/XocXoc/init');

let RongHo = require('../Controllers/game/RongHo/init');
let BanCa  = require('../Controllers/game/BanCa/Controller');
module.exports = function(io){
	io.users  = []; // danh sách người dùng đăng nhập
	io.admins = []; // danh sách admin đăng nhập
	
	io.game   = {
		xocxoc: new XocXoc(io), // thiết lập game xocxoc
		poker:    new Poker(),      // Quản lý phòng game Poker
		bacay:    new BaCay(),      // Quản lý phòng game Ba Cây
		fish:     new BanCa(), 
	};
	

	io.rongho   = new RongHo(io), // thiết lập game RongHo

	// Phát sóng tới tất cả người dùng và khách
	io.broadcast = function(data, noBroadcast = null){
	 
		this.clients.forEach(function(client){
			if (void 0 === client.admin && noBroadcast !== client) {
				client.red(data);
			}
		});
	};
	// Phát sóng tới tất cả  khách
	io.sendAllClient = function(data){
		this.clients.forEach(function(client){
			if (void 0 === client.admin && client.auth === false) {
				client.red(data);
			}
		});
	};
	// Phát sóng tới tất cả người dùng
	io.sendAllUser = function(data, noBroadcast = null){
		this.clients.forEach(function(client){
			if (void 0 === client.admin && client.auth === true && noBroadcast !== client) {
				client.red(data);
			}
		});
	};
	// Phát sóng tới tất cả người dùng
	io.sendAllAdmin = function(data, noBroadcast = null){
		this.clients.forEach(function(client){
			if (client.admin === true && client.auth === true && noBroadcast !== client) {
				client.red(data);
			}
		});
	};
	// Phát sóng tới tất cả khách
	io.sendInHome = function(data){
		io.clients.forEach(function(client){
			if (void 0 === client.admin && (client.auth === false || client.scene === 'home')) {
				client.red(data);
			}
		});
	};
};
