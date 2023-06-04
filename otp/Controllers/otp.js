let OTP = require('../Models/OTP')
let Phone = require('../Models/Phone')
let telegram = require('../Models/Telegram')
let Users = require('../Models/Users')
let helpers  = require('../Helpers/Helpers');
module.exports = (bot,msg) =>{
    telegram.findOne({'form':msg.from.id}, 'form uid phone', function(err3, teleCheck){
        var otp = (Math.random()*(9999-1000+1)+1000)>>0; // từ 1000 đến 9999
        if (!!teleCheck) {
            if (teleCheck.gift){
                let replyMarkup = bot.keyboard([
                    [bot.button('getOtp', 'OTP')]
                ], { resize: true });
                OTP.create({'uid':teleCheck.uid, 'phone':teleCheck.phone, 'code':otp, 'date':new Date()});
                bot.sendMessage(msg.from.id, `🙏 *OTP Q36.VIN của bạn là*: ${otp}\nThời hạn sử dụng: 30 giây `, {parseMode: 'markdown', replyMarkup:replyMarkup});
            }else{
                let replyMarkup = bot.keyboard([
                    [bot.button('getOtp', 'OTP')],
                    [bot.button('getGift', 'GIFTCODE')]
                ], { resize: true });
                OTP.create({'uid':teleCheck.uid, 'phone':teleCheck.phone, 'code':otp, 'date':new Date()});
                bot.sendMessage(msg.from.id, `🙏 *OTP Q36.VIN của bạn là*: ${otp}\nThời hạn sử dụng: 30 giây `, {parseMode: 'markdown', replyMarkup:replyMarkup});
            }
        }else{
            let replyMarkup = bot.keyboard([
                [bot.button('contact', '☎️ Chia sẻ số điện thoại')]
            ], { resize: true });
            bot.sendMessage(msg.from.id, `🙏 Quý khách vui lòng thao tác *CHIA SẺ SỐ ĐIỆN THOẠI* để lấy *OTP*`, {parseMode: 'markdown', replyMarkup:replyMarkup});
        }
    });
}