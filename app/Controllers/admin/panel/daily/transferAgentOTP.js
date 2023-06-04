var ChuyenRed = require('../../../../Models/ChuyenRed');
var UserInfo = require('../../../../Models/UserInfo');
var tab_DaiLy = require('../../../../Models/DaiLy');
let telegram = require('../../../../Models/Telegram');
var OTP = require('../../../../Models/OTP');
var Phone = require('../../../../Models/Phone');

var nTmux = require('node-tmux');
var validator = require('validator');
var Helper = require('../../../../Helpers/Helpers');
var request = require('request');

module.exports = function(req, res) {
 const { body, userAuth } = req || {}
 const { Data: data } = body || {};
 Phone.findOne({'uid':userAuth.id}, function(err3, check){
		if (check) {
			OTP.findOne({'uid': userAuth.id, 'phone':check.phone}, {}, {sort:{'_id':-1}}, function(err1, data){
				if (!data || ((new Date()-Date.parse(data.date))/1000) > 180 || data.active) {
					// Tạo mã OTP mới
					UserInfo.findOne({'id': userAuth.id}, 'red name', function(err2, user){
						if (user) {
		let otp = (Math.random()*(9999-1000+1)+1000)>>0; // OTP từ 1000 đến 9999
       let userNameOTP = user.name;
       let id = userAuth.id;
       let phoneno = check.phone;
           telegram.findOne({'phone':phoneno}, 'form', function(err3, teleCheck){
            if (!!teleCheck) {
                OTP.create({'uid':id, 'phone':phoneno, 'code':otp, 'date':new Date()}, function(err2, data2) {
                    redT.telegram.sendMessage(teleCheck.form, '*OTP*:  ' + otp + '', {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
                    if (data2) {
                        res.json({
                            status: 200,
                            success: true,
                            data: {
                                message: 'Mã OTP đã được gửi đến telegram của bạn'
                            }
                        });
                    }else{
                        res.json({
                            status: 200,
                            success: false,
                            data: {
                                message: 'Error 200'
                            }
                        });
                    }
                   });
            }else{
                res.json({
                    status: 200,
                    success: false,
                    data: {
                        message: 'Bạn cần kích hoạt otp để xử dụng tính năng này'
                    }
                });
            }
           });
					}});
				}else{
     UserInfo.findOne({'id': userAuth.id}, 'red name', function(err2, user2){
						if (user2) {
     let userNameOTP = user2.name;
     let id = userAuth.id;
     let phoneno = check.phone;
     let otp = (Math.random()*(9999-1000+1)+1000)>>0; // OTP từ 1000 đến 9999
      // App OTP
      telegram.findOne({'phone':phoneno}, 'form', function(err3, teleCheck){
        if (!!teleCheck) {
            OTP.create({'uid':id, 'phone':phoneno, 'code':otp, 'date':new Date()}, function(err2, data2) {
                redT.telegram.sendMessage(teleCheck.form, '*OTP*:  ' + otp + '', {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
                if (data2) {
                    res.json({
                        status: 200,
                        success: true,
                        data: {
                            message: 'Mã OTP đã được gửi đến telegram của bạn'
                        }
                    });
                }else{
                    res.json({
                        status: 200,
                        success: false,
                        data: {
                            message: 'Error 200'
                        }
                    });
                }
               });
        }else{
            res.json({
                status: 200,
                success: false,
                data: {
                    message: 'Bạn cần kích hoạt otp để xử dụng tính năng này'
                }
            });
        }
       });
    }
   });
  }
 });
		}else{
   res.json({
       status: 200,
       success: false,
       data: {
           message: 'Bạn cần kích hoạt otp để xử dụng tính năng này'
       }
   });
		}
	});
}
