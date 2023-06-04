
var tabTranDau   = require('../../../../Models/BongDa/BongDa');


module.exports = function(client) {
	//console.log('get data');
	tabTranDau.find({'blacklist':0}, function(err, data){
		//console.log(data);
		client.red({bongda:{data:data}});
	});
}