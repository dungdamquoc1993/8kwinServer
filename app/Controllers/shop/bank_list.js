const _ = require('lodash');
let request    = require('request');

module.exports = function(client){
	request.get({
		url: 'http://imopay.vnm.bz:10007/api/Bank/getBankAvailable?apiKey=ce7ce931-3037-4315-925e-eeb7483c2b0c',
	},
	function(err, httpResponse, body){
		//console.log(body);
			let data = JSON.parse(body);
			client.red({shop:{banking:{banklist:data.msg =="OK"?data.data:[]}}});
	});

}