let OTP = require('../Models/OTP')
let Phone = require('../Models/Phone')
let telegram = require('../Models/Telegram')
let Users = require('../Models/Users')
let helpers  = require('../Helpers/Helpers');
module.exports = (bot) =>{
    bot.on('contact', (msg) => {
        let phoneCrack = helpers.phoneCrack2(msg.contact.phone_number)
        if (phoneCrack) {
            Phone.findOne({'phone':phoneCrack},{},(err,checkPhone)=>{
                if (!!checkPhone){
                    telegram.findOne({'phone':phoneCrack}, 'form uid', function(err3, teleCheck){
                        if (!!teleCheck) {
                            if (teleCheck.gift){
                                let replyMarkup = bot.keyboard([
                                    [bot.button('getOtp', 'OTP')]
                                ], { resize: true });
                                bot.sendMessage(msg.from.id, `🙏 Bạn đã chia sẻ *SĐT*: ${phoneCrack}. Chọn *OTP* hoặc nhập *OTP* để lấy mã OTP mới`, {parseMode: 'markdown',replyMarkup:replyMarkup});
                            }else{
                                let replyMarkup = bot.keyboard([
                                    [bot.button('getOtp', 'OTP')],
                                    [bot.button('getGift', 'GIFTCODE')]
                                ], { resize: true });
                                bot.sendMessage(msg.from.id, `🙏 Bạn đã chia sẻ *SĐT*: ${phoneCrack}. Chọn *OTP* hoặc nhập *OTP* để lấy mã OTP mới`, {parseMode: 'markdown',replyMarkup:replyMarkup});
                            }
                        }else{
                            let replyMarkup = bot.keyboard([
                                [bot.button('getOtp', 'OTP')],
                                [bot.button('getGift', 'GIFTCODE')]
                            ], { resize: true });
                            telegram.create({'gift':false,'form':msg.from.id,'phone':phoneCrack,'uid':checkPhone.uid})
                            bot.sendMessage(msg.from.id, `🙏 Cám ơn bạn đã chia sẻ số điện thoại ☎️*${phoneCrack}📱*`, {parseMode: 'markdown',replyMarkup:replyMarkup});
                        }
                    });
                }else{
                    bot.sendMessage(msg.from.id, `Số điện thoại: ☎️*${phoneCrack.substring(2, phoneCrack.length)}📱*\n👉 Vui lòng quay lại game và nhập đúng số này để kích hoạt!`, {parseMode: 'markdown'}); 
                }
            })
        }
    })
}