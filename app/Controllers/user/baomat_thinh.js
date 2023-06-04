
var UserInfo  = require('../../Models/UserInfo');
var OTP       = require('../../Models/OTP');
var Phone     = require('../../Models/Phone');
let telegram = require('../../Models/Telegram');

var validator = require('validator');
var helper    = require('../../Helpers/Helpers');
var sms       = require('../../sms').sendOTP;

/*
function sendOTP(client, phone){
	// Gửi OTP kích hoạt
	if (!!phone && helper.checkPhoneValid(phone)) {
		var phoneCrack = helper.phoneCrack2(phone);
		if (phoneCrack) {
			UserInfo.findOne({'id': client.UID}, 'red otpFirst', function(err2, user){
				if (user) {
					Phone.findOne({'phone':phone}, function(err1, crack){
						if (crack) {
							client.red({notice:{title:'LỖI', text:'Số điện thoại đã tồn tại trên hệ thống.!'}});
						}else{
							var otp = (Math.random()*(9999-1000+1)+1000)>>0; // từ 1000 đến 9999
							OTP.findOne({'uid':client.UID, 'phone':phone}, {}, {sort:{'_id':-1}}, function(err2, data){
								if (!data || (new Date()-Date.parse(data.date))/1000 > 180 || data.active) {
									Phone.findOne({'uid':client.UID}, function(err3, check){
										if (check) {
											client.red({notice:{title:'LỖI', text:'Bạn đã kích hoạt OTP.!'}, user:{phone: check.phone}});
										}else{
											telegram.findOne({'phone':phone}, 'form', function(err3, teleCheck){
												if (!!teleCheck) {
													OTP.create({'uid':client.UID, 'phone':phone, 'code':otp, 'date':new Date()});
													client.red({notice:{title:'THÔNG BÁO', text:'Mã OTP đã được gửi tới Telegram của bạn.'}});
													client.redT.telegram.sendMessage(teleCheck.form, '*OTP*:  ' + otp + '', {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
												}else{
													client.red({notice:{title:'THẤT BẠI', text:'Bạn cần đăng ký Telegram để lấy OTP.'}});
												}
											});
										}
									});
								}else{
									client.red({notice:{title:'OTP', text:'Vui lòng mở ứng dụng telegram và kiểm tra tin nhắn.!'}});
								}
							});
						}
					});
				}
			});
		}else{
			client.red({notice:{title:'THÔNG BÁO', text:'Số điện thoại không hợp lệ.!'}});
		}
	}else{
		client.red({notice:{title:'THÔNG BÁO', text:'Số điện thoại không hợp lệ.!'}});
	}
}
*/

function sendOTP(client, data){
	let region = 0;
	var phone = 0;
	var type = data.type;
	var mavung = data.mavung;
	if (data.phone.substring(0, 1) != 0) {
		if (!!mavung) {
			switch (mavung) {
				case '1':
					region = 886;
					phone = region + data.phone;
					break;
				case '2':
					region = 84;
					phone = region + data.phone;
					break;
				case '3':
					region = 82;
					phone = region + data.phone;
					break;
				case '4':
					region = 81;
					phone = region + data.phone;
					break;
				case '0':
					client.red({notice:{title:'LỖI', text:'Vui lòng chọn lại mã vùng !'}});
					break;
			}
		}else{
			phone = data;
		}
	}else{client.red({notice:{title:'THÔNG BÁO', text:'Vui lòng bỏ số 0 ở đầu số.'}});}
	// Gửi OTP kích hoạt
	console.log('phone: '+phone);
	if (!!phone && helper.checkPhoneValid(phone)) {
		var phoneCrack = helper.phoneCrack2(phone);
		if (phoneCrack) {
			UserInfo.findOne({'id': client.UID}, 'red otpFirst', function(err2, user){
				if (user) {
					Phone.findOne({'phone':phone}, function(err1, crack){
						if (crack) {
							client.red({notice:{title:'LỖI', text:'Số điện thoại đã tồn tại trên hệ thống 1.!'}});
						}else{
							var otp = (Math.random()*(9999-1000+1)+1000)>>0; // từ 1000 đến 9999
							OTP.findOne({'uid':client.UID, 'phone':phone}, {}, {sort:{'_id':-1}}, function(err2, data){
								if (!data || (new Date()-Date.parse(data.date))/1000 > 180 || data.active) {
									Phone.findOne({'uid':client.UID}, function(err3, check){
										if (check) {
											client.red({notice:{title:'LỖI', text:'Bạn đã kích hoạt OTP.!'}, user:{phone: check.phone}});
										}else{
											if (type == '1') {
												client.red({notice:{title:'THÔNG BÁO', text:'Vui lòng đăng ký SMS trước.'}});
											}else{
												if (user.otpFirst) {
													if (user.red < 2000) {
														client.red({notice:{title:'THÔNG BÁO', text:'Số dư không khả dụng.'}});
													}else{
														sms(phone, otp, mavung);
														OTP.create({'uid':client.UID, 'phone':phone, 'code':otp, 'date':new Date()});
														UserInfo.updateOne({id:client.UID}, {$inc:{red:-2000}}).exec();
														client.red({notice:{title:'THÔNG BÁO', text:'Mã OTP đã được gửi tới số điện thoại của bạn.'}});
													}
												}else{
													sms(phone, otp, mavung);
													OTP.create({'uid':client.UID, 'phone':phone, 'code':otp, 'date':new Date()});
													UserInfo.updateOne({id:client.UID}, {$set:{otpFirst:true}}).exec();
													client.red({notice:{title:'THÔNG BÁO', text:'Mã OTP đã được gửi tới số điện thoại của bạn.'}});
												}
											}
											
										}
									});
								}else{
									client.red({notice:{title:'OTP', text:'Vui lòng mở điện thoại và kiểm tra tin nhắn.!'}});
								}
							});
						}
					});
				}
			});
		}else{
			client.red({notice:{title:'THÔNG BÁO', text:'Số điện thoại không hợp lệ.!'}});
		}
	}else{
		client.red({notice:{title:'THÔNG BÁO', text:'Vui lòng bỏ số 0 ở đầu số.'}});
	}
}

function OTPTele(client, data){
	let region = 0;
	var phone = 0;
	console.log(data);
	if (!!mavung) {
		switch (mavung) {
			case '1':
				region = 886;
				phone = region + data.phone;
				break;
			case '2':
				region = 84;
				phone = region + data.phone;
				break;
			case '3':
				region = 82;
				phone = region + data.phone;
				break;
			case '4':
				region = 81;
				phone = region + data.phone;
				break;
			case '0':
				client.red({notice:{title:'LỖI', text:'Vui lòng chọn lại mã vùng !'}});
				break;
		}
		
	}else{

		phone = data;
	}
	// Gửi OTP kích hoạt
	if (!!data && helper.checkPhoneValid(phone)) {
		var phoneCrack = helper.phoneCrack2(phone);
		if (phoneCrack) {
			UserInfo.findOne({'id': client.UID}, 'red otpFirst', function(err2, user){
				if (user) {
					Phone.findOne({'phone':phone}, function(err1, crack){
						if (crack) {
							client.red({notice:{title:'LỖI', text:'Số điện thoại đã tồn tại trên hệ thống 2.!'}});
						}else{
							var otp = (Math.random()*(9999-1000+1)+1000)>>0; // từ 1000 đến 9999
							OTP.findOne({'uid':client.UID, 'phone':phone}, {}, {sort:{'_id':-1}}, function(err2, data){
								if (!data || (new Date()-Date.parse(data.date))/1000 > 180 || data.active) {
									Phone.findOne({'uid':client.UID}, function(err3, check){
										if (check) {
											client.red({notice:{title:'LỖI', text:'Bạn đã kích hoạt OTP.!'}, user:{phone: check.phone}});
										}else{
											telegram.findOne({'phone':phone}, 'form uid phone', function(err3, teleCheck){
												if (teleCheck) {
													client.red({notice:{title:'OTP', text:'Vui lòng mở ứng dụng telegram và kiểm tra tin nhắn.!'}});
													console.log(teleCheck.phone);
												}else{
													telegram.findOne({'form':client.UID}, function(err4, teleCheck1){
														if (teleCheck1) {
															teleCheck1.phone = phone;
															teleCheck1.save();
														}else{
															telegram.create({'form':client.UID,'phone':phone,'uid':client.UID});
															
														}
													});
												}
											});
										}
									});
								}else{
									client.red({notice:{title:'OTP', text:'Vui lòng mở ứng dụng telegram và kiểm tra tin nhắn.!'}});
								}
							});
						}
					});
				}
			});
		}else{
			client.red({notice:{title:'THÔNG BÁO', text:'Số điện thoại không hợp lệ.!'}});
		}
	}else{
		client.red({notice:{title:'THÔNG BÁO', text:'Số điện thoại không hợp lệ.!'}});
	}
}

function regOTP(client, data){
	let region = 0;
	var phone = 0;
	var type = data.type;
	var mavung = data.mavung;
	if (!!mavung) {
		switch (mavung) {
			case '1':
				region = 886;
				phone = region + data.phone;
				break;
			case '2':
				region = 84;
				phone = region + data.phone;
				break;
			case '3':
				region = 82;
				phone = region + data.phone;
				break;
			case '4':
				region = 81;
				phone = region + data.phone;
				break;
			case '0':
				client.red({notice:{title:'LỖI', text:'Vui lòng chọn lại mã vùng !'}});
				break;
		}
		
	}else{

		phone = data;
	}

	if (!!data && !!data.phone && !!data.otp) {
		if (!helper.checkPhoneValid(phone)) {
			client.red({notice: {title:'LỖI', text: 'Số điện thoại không hợp lệ'}});
		} else if (!validator.isLength(data.otp, {min: 4, max: 6}) && false){//ignore otp
			client.red({notice: {title:'LỖI', text: 'Mã OTP Không đúng!!'}});
		} else {
			var phoneCrack = phone;
			console.log(phoneCrack);
			console.log(1111111);
			if (phoneCrack) {
				OTP.findOne({'uid':client.UID, 'phone':phoneCrack}, {}, {sort:{'_id':-1}}, function(err1, data_otp){

					if (true) {//data_otp && data.otp == data_otp.code
						if (false) {//((new Date()-Date.parse(data_otp.date))/1000) > 180 || data_otp.active
							client.red({notice:{title:'LỖI', text:'Mã OTP đã hết hạn.!'}});
						}else{

							UserInfo.findOne({'id': client.UID}, 'red xu phone email cmt', function(err2, dU){
								if (dU) {
									Phone.findOne({'phone':phoneCrack}, function(err3, crack){
										if (crack) {
											client.red({notice:{title:'LỖI', text:'Số điện thoại đã tồn tại trên hệ thống.!'}});
										}else{
											Phone.findOne({'uid':client.UID}, function(err4, check){
												if (check) {
													client.red({notice:{title:'LỖI', text:'Bạn đã kích hoạt OTP.!'}, user:{phone: phoneCrack}});
												}else{
													// Xác thực thành công
													// data_otp.active = true;
													// data_otp.save();
													try {
														Phone.create({'uid':client.UID, 'phone':phoneCrack, 'region':phoneCrack}, function(err, cP){
															if (!!cP) {
																UserInfo.updateOne({id:client.UID}, {$set:{email:'', cmt:'', otpGet:0}, $inc:{red:0, xu:0}}).exec();
																client.red({notice:{title:'THÀNH CÔNG', text: 'Xác thực thành công.!' + '\n' + 'Chúc bạn chơi game vui vẻ...'}, user: {red: dU.red*1+0, xu: dU.xu*1+0, phone: phoneCrack, email: '', cmt: ''}});
															}else{
																client.red({notice:{title:'LỖI', text:'Số điện thoại đã tồn tại trên hệ thống.!'}});
															}
														});
													} catch (error) {
														client.red({notice:{title:'LỖI', text:'Số điện thoại đã tồn tại trên hệ thống.!'}});
													}
												}
											});
										}
									});
								}
							});
						}
					}else{
						client.red({notice:{title:'LỖI', text:'Mã OTP Không đúng.!'}});
					}
				});
			}else{
				client.red({notice:{title:'THÔNG BÁO', text:'Số điện thoại không hợp lệ.!'}});
			}
		}
	}
}

module.exports = function(client, data) {
	if (!!data) {
		if (!!data.sendOTP) {
			sendOTP(client, data.sendOTP);
		}
		if (!!data.regOTP) {
			regOTP(client, data.regOTP);
		}
		if (!!data.OTPTele) {
			OTPTele(client, data.OTPTele);
		}
	}
}
