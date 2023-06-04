
let start   = require('./model/start');
let otp     = require('./model/otp');
let contact = require('./model/contact');
let giftcode = require('./model/giftcode');
let telegram = require('../Models/Telegram');
let helpers  = require('../Helpers/Helpers');

module.exports = function(bot, msg) {
	let text = msg.text;
	if(/^otp$/i.test(text)){
		otp(bot, msg.from.id);
	}else if(/^giftcode$/i.test(text)){
		giftcode(bot, msg.from.id);
	}else if(msg.contact){
		contact(bot, msg.from.id, msg.contact.phone_number);
		/*
		let phoneCrack = helpers.phoneCrack(msg.contact.phone_number);
		telegram.create({'form':msg.from.id, 'phone':phoneCrack.phone}, function(err, cP){
						if (!!cP) {
							bot.sendMessage(msg.from.id, '_Đăng nhập thành công_', {parse_mode: 'markdown',reply_markup: {remove_keyboard: true}});
							bot.sendMessage(msg.from.id, '*HƯỚNG DẪN*' + '\n\n' + 'Nhập:' + '*OTP*:        Lấy mã OTP miễn phí.' + '\n' + '', {parse_mode: 'markdown',reply_markup: {remove_keyboard: true}});
						}else{
							bot.sendMessage(msg.from.id, '_Thao tác thất bại_', {parse_mode: 'markdown',reply_markup: {remove_keyboard: true}});
						}
					});
					*/
	}else{
		start(bot, msg.from.id);
	}
}
