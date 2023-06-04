
var ChuyenRed = require('../../Models/ChuyenRed');
var UserInfo  = require('../../Models/UserInfo');
var tab_DaiLy = require('../../Models/DaiLy');
var OTP       = require('../../Models/OTP');
var Phone     = require('../../Models/Phone');
let telegram = require('../../Models/Telegram');
let UserMission = require('../../Models/UserMission');
var validator = require('validator');
var Helper    = require('../../Helpers/Helpers');
//let Push    = require('../../Models/Push');


module.exports = function(client, data){
	if (!!data && !!data.name && !!data.otp) {
		if (!validator.isLength(data.name, {min: 3, max: 17})) {
			client.red({notice: {title: 'LỖI', text: 'Tên nhân vật không hợp lệ.!'}});
		}else if (!validator.isLength(data.otp, {min: 4, max: 6})) {
			client.red({notice: {title: 'LỖI', text: 'Mã OTP không hợp lệ.!'}});
		}else{
			var red  = data.red>>0;
			var name = ''+data.name+'';
			var otp  = data.otp;

			if(validator.isEmpty(name) ||
				red < 10000 ||
				name.length > 17 ||
				name.length < 3 ||
				otp.length != 4)
			{
				client.red({notice:{title:'CHUYỂN Tiền', text:'Kiểm tra lại các thông tin.!'}});
			}else{
				Phone.findOne({'uid':client.UID}, {}, function(err, check){
				  
					if (check) {
						OTP.findOne({'uid':client.UID, 'phone':check.phone}, {}, {sort:{'_id':-1}}, function(err, data_otp){
							if (data_otp && data.otp == data_otp.code) {
								if (((new Date()-Date.parse(data_otp.date))/1000) > 180 || data_otp.active) {
									client.red({notice:{title:'LỖI', text:'Mã OTP đã hết hạn.!'}});
								}else{
									name = name.toLowerCase();
									var active1 = tab_DaiLy.findOne({$or:[
										{nickname:name},
										{nickname:client.profile.name}
									]}).exec();

									var active2 = UserInfo.findOne({name:name}, 'id name red').exec();
									var active3 = UserInfo.findOne({id:client.UID}, 'red block').exec();
									Promise.all([active1, active2, active3])
									.then(valuesCheck => {
										var daily = valuesCheck[0];
										var to    = valuesCheck[1];
										var user  = valuesCheck[2];
										if (!!to) {
											if (to.id == client.UID) {
												client.red({notice:{title:'CHUYỂN TIỀN',text:'Bạn không thể chuyển cho chính mình.!!'}});
											}else{
												if (user == null || (user.red-10000 < red)) {
													client.red({notice:{title:'CHUYỂN TIỀN',text:'Số dư không khả dụng.!!'}});
												}else{
													if (user.block) {
														client.red({notice:{title:'CHUYỂN TIỀN',text:'Bạn không thể chuyển Tiền!!'}});
													}else{
														UserInfo.findOneAndUpdate({id: client.UID}, {$inc:{red:-red}}, function (err, result) {
                                                            if (!!result) {

																client.red({notice:{title:'CHUYỂN TIỀN', text: 'Giao dịch thành công.!!'}, user:{red:user.red-red}});
																//Push.create({
																//type:"ChuyenTien",
																//data:JSON.stringify({name:user.name,money:tongTien,date:new Date()})
																//});
																telegram.findOne({'phone':check.phone}, 'form', function(err3, teleCheck){
																	if (!!teleCheck) {
																		let text = `*CHUYỂN XU*\n👉Bạn đã *chuyển* ${Helper.numberWithCommas(red)} XU tới người chơi: *${to.name}*\n👉Nội dung: *${data.message}*\n👉Số dư: ${Helper.numberWithCommas(user.red-red)}`;
																		client.redT.telegram.sendMessage(teleCheck.form, text, {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
																	}
																});

															}
															else {
                                                                console.log(err);
                                                            }
                                                        });
													//UserInfo.updateOne({id: client.UID}, {$inc:{red:-red}}).exec();
													
													var thanhTien = !!daily ? red : Helper.anPhanTram(red, 1, 2);
													var create = {'from':client.profile.name, 'to':to.name, 'red':red, 'red_c':thanhTien, 'time': new Date()};
													if (void 0 !== data.message && !validator.isEmpty(data.message.trim())) {
														create = Object.assign(create, {message: data.message});
													}
													Phone.findOne({'uid':to.id}, {}, function(err, check2){
														if (check2) {
															telegram.findOne({'phone':check2.phone}, 'form', function(err3, teleCheck2){
																if (!!teleCheck2) {
																	let text = `*CHUYỂN XU*\n👉Bạn đã *nhận* ${Helper.numberWithCommas(thanhTien)} XU từ: *${client.profile.name}*\n👉Nội dung: *${data.message}*\n👉Số dư: ${Helper.numberWithCommas(to.red*1+thanhTien)}`;
																	client.redT.telegram.sendMessage(teleCheck2.form, text, {parse_mode:'markdown', reply_markup:{remove_keyboard: true}});
																}
															});
														}else{
															console.log(`${to.name} chua kich hoat bao mat sdt`);
														}
														
													});
													ChuyenRed.create(create);
													UserInfo.findOneAndUpdate({name: to.name}, {$inc:{red:thanhTien}}, function (err2, result2) {
														if (!!result2) {
															if (!!daily) {
																if (result2.red < thanhTien) {
																	result2.daily = client.profile.name;
																	result2.save();
																}
																let nhiemVu = red;
																	if (nhiemVu > parseInt(10000000))
																		nhiemVu = parseInt(10000000);
																	UserMission.updateOne({ uid: result2.id, name: result2.name, type: 4, active: false, achived: false }, { $set: { active: true, totalPay: nhiemVu, totalAchive: nhiemVu * global.SKnapthe, current: 0, achived: false, time: new Date((new Date()).getTime() + 1728000000) } }).exec();
															}
															if (void 0 !== client.redT.users[to.id]) {
																Promise.all(client.redT.users[to.id].map(function(obj){
																	obj.red({notice:{title:'CHUYỂN Tiền', text:'Bạn nhận được ' + Helper.numberWithCommas(thanhTien) + ' XU.' + '\n' + 'Từ người chơi: ' + client.profile.name}, user:{red: to.red*1+thanhTien}});
																}));
															}
															
															OTP.updateOne({'_id':data_otp._id.toString()}, {$set:{'active':true}}).exec();
														}else {
															console.log(err2);
														}
													});

													//UserInfo.updateOne({name: to.name}, {$inc:{red:thanhTien}}).exec();
												}
											}
										}
										}else{
											client.red({notice:{title:'CHUYỂN TIỀN',text:'Người dùng không tồn tại.!!'}});
										}
									})
								}
							}else{
								client.red({notice:{title:'LỖI', text:'Mã OTP Không đúng.!'}});
							}
						});
					}else{
						client.red({notice:{title: 'THÔNG BÁO', text: 'Chức năng chỉ dành cho tài khoản đã kích hoạt.'}});
					}
				});
			}
		}
	}
}
