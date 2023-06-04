
let Poker  = require('../Controllers/game/poker/Controller');
let BaCay  = require('../Controllers/game/BaCay/Controller');

let XocXoc = require('../Controllers/game/XocXoc/init');

let RongHo = require('../Controllers/game/RongHo/init');

module.exports = function(io){
    console.log(io);
    io.game   = {
		xocxoc: new XocXoc(io), // thiết lập game xocxoc
		poker:    new Poker(),      // Quản lý phòng game Poker
		bacay:    new BaCay(),      // Quản lý phòng game Ba Cây
	};
}