
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
	console.log(data);
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
																//telegram.create({'form':client.UID,'phone':phone,'uid':client.UID});
																Phone.create({'uid':client.UID, 'phone':phone, 'region':'0'});
																client.red({notice:{title:'OTP', text:'Vui lòng mở ứng dụng telegram và chia chia sẻ số điện thoại rồi lấy OTP.!'}});
															}
														});
													}
												});
											}else{
												if (user.red < 2000) {
													client.red({notice:{title:'THÔNG BÁO', text:'Số dư không khả dụng.'}});
												}else{
													sms(phone, otp, mavung);
													OTP.create({'uid':client.UID, 'phone':phone, 'code':otp, 'date':new Date()});
													UserInfo.updateOne({id:client.UID}, {$inc:{red:-2000}}).exec();
													Phone.create({'uid':client.UID, 'phone':phone, 'region':'0'});
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
		client.red({notice:{title:'THÔNG BÁO', text:'Số điện thoại không hợp lệ.!'}});
	}
}

function regOTP(client, data){
	let region = 0;
	var phone = 0;
	var type = data.type;
	var mavung = data.mavung;
	console.log('regOTP'+mavung)
	//if (!!mavung) {
		if (false) {
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
	console.log(data);
	if (!!data && !!data.phone && !!data.otp) {
		console.log(phone);
		console.log(helper.checkPhoneValid(phone));
		//if (!helper.checkPhoneValid(phone)) {
			if (false) {
			console.log(1111);
			client.red({notice: {title:'LỖI', text: 'Số điện thoại không hợp lệ'}});
		// } else if (!validator.isLength(data.otp, {min: 4, max: 6}) && false){//ignore otp
		// 	client.red({notice: {title:'LỖI', text: 'Mã OTP Không đúng!!'}});
		} else {
			console.log(22222);
			var phoneCrack = phone;
			console.log(phoneCrack);
			if (phoneCrack) {
				OTP.findOne({'uid':client.UID, 'phone':phoneCrack}, {}, {sort:{'_id':-1}}, function(err1, data_otp){

					if (true) {//data_otp && data.otp == data_otp.code
						if (false) {//((new Date()-Date.parse(data_otp.date))/1000) > 180 || data_otp.active
							client.red({notice:{title:'LỖI', text:'Mã OTP đã hết hạn.!'}});
						}else{
													// Xác thực thành công
													//data_otp.active = true;
													//data_otp.save();
													
													phoneCrack.region = '84';
													if (phoneCrack.region == '0' || phoneCrack.region == '84') {
														phoneCrack.region = '+84';
													}
													phoneCrack.phone = phoneCrack.phone.substring(1);
													Phone.findOne({'phone':phoneCrack.phone}, function(err3, crack){
														if (crack) {
															client.red({notice:{title:'LỖI', text:'Số điện thoại đã tồn tại trên hệ thống.!'}});
														}else{
															Phone.findOne({'uid':client.UID}, function(err4, check){
																if (check) {
																	client.red({user:{phone:helper.cutPhone(check.region+check.phone)}});
																}else{
																	try {
																		phoneCrack.region = '+84';
																		
																		Phone.create({'uid':client.UID, 'phone':phoneCrack.phone, 'region':'+84'}, function(err, cP){
																			if (!!cP) {
																				//client.red({user:{phone:helper.cutPhone(phone)}});
																				UserInfo.updateOne({id:client.UID}, {$set:{email:'', cmt:'', otpGet:0}, $inc:{red:0, xu:0}}).exec();
																				client.red({notice:{title:'THÀNH CÔNG', text: 'Xác thực thành công.!' + '\n' + 'Chúc bạn chơi game vui vẻ...'}, user: {phone: phoneCrack.phone, email: '', cmt: ''}});
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

function Delete(client, data){
	console.log(data);
	telegram.findOne({uid:client.UID}, {}, function(err1, crack){
		if (crack) {
			OTP.findOne({'uid':client.UID, 'phone':crack.phone}, {}, {sort:{'_id':-1}}, function(err1, data_otp){
				if (true) {//data_otp && data.otp == data_otp.code
					if (false) {//((new Date()-Date.parse(data_otp.date))/1000) > 180 || data_otp.active
						client.red({notice:{title:'', text:'Mã OTP đã hết hạn.!'}});
					}else{
						// data_otp.active = true;
						// data_otp.save();
						UserInfo.updateOne({id:client.UID}, {$set:{email:'', cmt:'', otpGet:0}, $inc:{red:0, xu:0}}).exec();
						Phone.deleteOne({uid:client.UID},function(err,result){
							telegram.deleteOne({'phone':crack.phone}, function(err2,result){
								if (!err2) {
									client.red({notice:{title:'', text:'Xoá sđt thành công!'}, user:{phone: ''}});
								} else {
									client.red({notice:{title:'', text:'Không thể xoá sđt'}});
								}
							})
						   })
					}
				}else{
					client.red({notice:{title:'', text:'Mã OTP Không đúng.!'}});
				}
			});
		}else{
			Phone.deleteOne({uid:client.UID},function(err,result){
				telegram.deleteOne({'uid':client.UID}, function(err2,result){
					if (!err2) {
						client.red({notice:{title:'', text:'Xoá sđt thành công!'}, user:{phone: ''}});
					} else {
						client.red({notice:{title:'', text:'Không thể xoá sđt'}});
					}
				})
			   })
		}
	})
	
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
		if (!!data.delete) {
			Delete(client, data.delete);
		}
	}
}
