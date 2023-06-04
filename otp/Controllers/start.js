let OTP = require('../Models/OTP')
let Phone = require('../Models/Phone')
let telegram = require('../Models/Telegram')
let Users = require('../Models/Users')
let helpers  = require('../Helpers/Helpers');
module.exports = (bot) =>{
    let parseMode= 'markdown'
    bot.on('text', (msg) => {
        if (msg.text.toLowerCase() =="/start" || msg.text.toLowerCase() =="start"){
            telegram.findOne({'form':msg.from.id}, 'phone', function(err, data){
                if (data) {
                    let replyMarkup = bot.keyboard([
                        [bot.button('getOtp', 'OTP')],
                        [bot.button('getGift', 'GIFTCODE')]
                    ], { resize: true });
                    let ChatText = `*🎯HƯỚNG DẪN🎯* \n👉 *Bảo Mật: *✅\n👉 *SĐT: *${data.phone}📱\n👉 Nhập *OTP* để nhận mã OTP mới\n👉 Nhập *GiftCode* Nhận ngay GiftCode khởi nghiệp.`
                    bot.sendMessage(msg.from.id, ChatText, {parseMode:parseMode, replyMarkup:replyMarkup});
                }else{
                    let replyMarkup = bot.keyboard([
                        [bot.button('contact', '☎️ Chia sẻ số điện thoại')]
                    ], { resize: true });
                    bot.sendMessage(msg.from.id, '🎲*Q36.VIN*🎲  Đây là lần đầu tiên bạn sử dụng App OTP.\n👉 Bạn vui lòng ấn ☎️*CHIA SẺ SỐ ĐIỆN THOẠI* để lấy mã OTP miễn phí.', {parseMode:parseMode, replyMarkup:replyMarkup});
                }
            });
        }
        if (msg.text.toLowerCase() =="/otp" || msg.text.toLowerCase() =="otp"){
            require('./otp')(bot,msg)
        }
        if (msg.text.toLowerCase() =="/giftcode" || msg.text.toLowerCase() =="giftcode"){
            require('./giftcode')(bot,msg)
        }
    })
}