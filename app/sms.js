
/**
 * SMS Controller
 */

let request = require('request');
let config  = require('../config/sms');

let sendOTP = function(phone, otp, region){
	let request_id = ''+Math.floor(Math.random() * Math.floor(999999));
	let brandname = 'VT DI DONG';
	let username = 'UNCLEHOVN1';
	
	if (region == '2') {
		brandname = 'VT DI DONG';
		username = 'UNCLEHOVN1';
	}else{
		brandname = 'INTERGO';
		username = 'UNCLEHOVN1';
	}
	let form = {
		"phone": phone,
		"password": config.Password, // pass
		"message": config.Messages + otp,
		"idrequest": request_id,
		"brandname": brandname, // Brandname
		"username": username, //username
	};
	console.log(form);
	request.post({
		url: config.linkAPI,
		headers: {'Content-Type': 'application/json'},
		json: form,
	},function(dataResponse){
		console.log(dataResponse);
	});
}

module.exports = {
	sendOTP: sendOTP,
}
