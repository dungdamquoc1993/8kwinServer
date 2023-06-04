
var UserInfo = require('../../Models/UserInfo');
var OTP      = require('../../Models/OTP');
var Phone    = require('../../Models/Phone');
let telegram = require('../../Models/Telegram');

var Helper   = require('../../Helpers/Helpers');

function gui(client, red){
	red = red>>0;
	if (red < 10000) {
		client.red({notice:{title: 'GỬI XU', text: 'Số tiền gửi phải lớn hơn 10.000'}});
	}else{
		Phone.findOne({'uid':client.UID}, {}, function(err3, check){
			check = true;
			if (check) {
				UserInfo.findOne({id: client.UID}, 'red ketSat', function(err, user){
					if(user){
						if (user.red < red) {
							client.red({notice:{title: 'THÔNG BÁO', text: 'Số dư không khả dụng.'}});
						}else{
							UserInfo.updateOne({id: client.UID}, {$inc:{red: -red, ketSat: red}}).exec();
							client.red({notice:{title:'THÀNH CÔNG', text: 'Đã gửi ' + Helper.numberWithCommas(red) + ' XU vào két sắt thành công.!!'}, user:{red:user.red-red, ketSat: user.ketSat*1+red}});
							telegram.findOne({'phone':check.phone}, 'form', function(err3, teleCheck){
								if (!!teleCheck) {
									let text = `*KÉT SẮT*\n👉Bạn đã *gửi* ${Helper.numberWithCommas(red)} XU vào két sắt.\n👉Trong két: ${Helper.numberWithCommas(user.ketSat*1+red)} XU.\n👉Số dư: ${Helper.numberWithCommas(user.red-red)}`;
									client.redT.telegram.sendMessage(teleCheck.form, text, {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
								}
							});
						}
					}
				});
			}else{
				client.red({notice:{title: 'THÔNG BÁO', text: 'Chức năng chỉ dành cho tài khoản đã kích hoạt.'}});
			}
		});
	}
}

function rut(client, data){
	var red = data.red>>0;

	if (red < 10000) {
		client.red({notice:{title: 'RÚT XU', text: 'Số tiền rút phải lớn hơn 10.000'}});
	}else{
		Phone.findOne({'uid':client.UID}, {}, function(err3, check){
			check = true;
			if (check) {
				UserInfo.findOne({id:client.UID}, 'red ketSat phone', function(err, user){
					if(user){
						OTP.findOne({'uid':client.UID, 'phone':check.phone}, {}, {sort:{'_id':-1}}, function(err, data_otp){
							if (true) {//data_otp && data.otp == data_otp.code
								if (false) {//((new Date()-Date.parse(data_otp.date))/1000) > 180 || data_otp.active
									client.red({notice:{title:'LỖI', text:'Mã OTP đã hết hạn.!'}});
								}else{
									if (user.ketSat < red) {
										client.red({notice:{title: 'THẤT BẠI', text: 'Số tiền trong két nhỏ hơn số tiền giao dịch.'}});
									}else{
										//OTP.updateOne({'_id': data_otp._id.toString()}, {$set:{'active':true}}).exec();
										UserInfo.updateOne({id: client.UID}, {$inc:{red: red, ketSat: -red}}).exec();
										client.red({notice:{title:'THÀNH CÔNG', text: 'Rút thành công ' + Helper.numberWithCommas(red) + ' XU từ két sắt.!!'}, user:{red: user.red*1+red, ketSat: user.ketSat-red}});
										telegram.findOne({'phone':check.phone}, 'form', function(err3, teleCheck){
											if (!!teleCheck) {
												let text = `*KÉT SẮT*\n👉Bạn đã *rút* ${Helper.numberWithCommas(red)} XU vào két sắt.\n👉Trong két: ${Helper.numberWithCommas(user.ketSat-red)} XU.\n👉Số dư: ${Helper.numberWithCommas(user.red*1+red)}`;
												client.redT.telegram.sendMessage(teleCheck.form, text, {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
											}
										});
									}
								}
							}else{
								client.red({notice:{title:'LỖI', text:'Mã OTP Không đúng.!'}});
							}
						});
					}
				});
			}else{
				client.red({notice:{title: 'THÔNG BÁO', text: 'Chức năng chỉ dành cho tài khoản đã kích hoạt.'}});
			}
		});
	}
}

module.exports = function(client, data) {
	if (void 0 !== data.gui) {
		gui(client, data.gui)
	}
	if (void 0 !== data.rut) {
		rut(client, data.rut)
	}
};
