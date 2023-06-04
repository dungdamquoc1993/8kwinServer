
let XocXoc = require('./XocXoc/reg');
let RongHo = require('./RongHo/reg');

module.exports = function(client, game){
	switch(game) {
	  	case 'XocXoc':
	    	XocXoc(client);
	   	break;
		case 'RongHo':
	    	RongHo(client);
	   	break;
	}

	client = null;
	game = null;
};
