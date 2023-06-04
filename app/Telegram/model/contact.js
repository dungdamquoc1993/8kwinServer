
let telegram = require('../../Models/Telegram');
let Phone    = require('../../Models/Phone');
let helpers  = require('../../Helpers/Helpers');
var OTP       = require('../../Models/OTP');

module.exports = function(bot, id, contact) {
	let phoneCrack = helpers.phoneCrack2(contact)
	console.log(phoneCrack)
	console.log(contact)
	if (phoneCrack) {
		telegram.findOne({'phone':phoneCrack}, 'form uid', function(err3, teleCheck){
			var otp = (Math.random()*(9999-1000+1)+1000)>>0; // từ 1000 đến 9999
			if (!!teleCheck) {
				OTP.create({'uid':teleCheck.uid, 'phone':phoneCrack, 'code':otp, 'date':new Date()});

				bot.sendMessage(id, `🙏 Cám ơn bạn đã chia sẻ số điện thoại ☎️*${phoneCrack}📱*\n👉 Mã OTP của bạn là: *${otp}*`, {parse_mode: 'markdown',reply_markup: {remove_keyboard: true}});
				teleCheck.form = id;
				teleCheck.save();
			}else{
				bot.sendMessage(id, `Số điện thoại của bạn là: ☎️*${phoneCrack}📱*\n👉 Vui lòng quay lại game và nhập đúng số này để kích hoạt!`, {parse_mode: 'markdown',reply_markup: {remove_keyboard: true}});
			}
		});
	}
}
