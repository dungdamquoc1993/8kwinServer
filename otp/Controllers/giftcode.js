let OTP = require('../Models/OTP')
let Phone = require('../Models/Phone')
let telegram = require('../Models/Telegram')
let Users = require('../Models/Users')
let helpers  = require('../Helpers/Helpers');
let GiftCode = require('../Models/GiftCode');
let shortid  = require('shortid');
let Message = require('../Models/Message');
module.exports = (bot,msg) =>{
    let id = msg.from.id
    telegram.findOne({'form':id}, {}, function(err1, check){
		if (check) {
			Phone.findOne({'phone':check.phone}, {}, function(err2, checkPhone){
				if (checkPhone) {
					if (!check.gift) {
						// Gift khởi nghiệp
						let get_gift = shortid.generate();
						get_gift = get_gift.toLowerCase();
						try {
							GiftCode.create({'code':get_gift, 'red':10000, 'xu':0, 'type':get_gift, 'date': new Date(), 'todate':new Date(new Date()*1+86400000), 'to':checkPhone.uid}, function(err3, gift){
								if (!!gift){
									if (!check.gift){
										check.gift = true;
										check.save();
										bot.sendMessage(id, `Chúc mừng bạn đã nhận Giftcode khởi nghiệp, mã Giftcode của bạn là: *${get_gift}*`, {parseMode:'markdown',reply_markup:{remove_keyboard:true}});
										Message.create({'uid': checkPhone.uid, 'title':'Giftcode khởi nghiệp', 'text':'Chúc mừng bạn đã nhận Giftcode khởi nghiệp, mã Giftcode của bạn là: '+ get_gift, 'time':new Date()});
									}else{
										bot.sendMessage(id, `Chúc mừng bạn đã nhận Giftcode khởi nghiệp, mã Giftcode của bạn là: *${get_gift}*`, {parseMode:'markdown',reply_markup:{remove_keyboard:true}});
										Message.create({'uid': checkPhone.uid, 'title':'Giftcode khởi nghiệp', 'text':'Chúc mừng bạn đã nhận Giftcode khởi nghiệp, mã Giftcode của bạn là: '+ get_gift, 'time':new Date()});
									}
								}else{
									bot.sendMessage(id, '_Hãy quay lại vào ngày hôm sau._', {parseMode:'markdown', reply_markup:{remove_keyboard:true}});
								}
							});
						} catch (error) {
							bot.sendMessage(id, '_Hãy quay lại vào ngày hôm sau._', {parseMode:'markdown', reply_markup:{remove_keyboard:true}});
						}
					}else{
						bot.sendMessage(id, '_Hãy quay lại vào ngày hôm sau._', {parseMode:'markdown', reply_markup:{remove_keyboard:true}});
					}
				}else{
					bot.sendMessage(id, '_Thao tác thất bại, không thể đọc số điện thoại_', {parseMode:'markdown', reply_markup:{remove_keyboard:true}});
				}
			});
		}else{
			bot.sendMessage(id, '_Thao tác thất bại, không thể đọc số điện thoại_', {parseMode:'markdown', reply_markup:{remove_keyboard:true}});
		}
	});
}