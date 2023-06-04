
let validator = require('validator');
let User      = require('./app/Models/Users');
let UserInfo = require('./app/Models/UserInfo');
let helpers   = require('./app/Helpers/Helpers');
let socket    = require('./app/socket.js');
let TaiXiu_User     = require('./app/Models/TaiXiu_user');
let MiniPoker_User  = require('./app/Models/miniPoker/miniPoker_users');
let Bigbabol_User   = require('./app/Models/BigBabol/BigBabol_users');
let VQRed_User      = require('./app/Models/VuongQuocRed/VuongQuocRed_users');
let TamHung_User    = require('./app/Models/TamHung/TamHung_users');
let Zeus_User      = require('./app/Models/Zeus/Zeus_user');
let BauCua_User     = require('./app/Models/BauCua/BauCua_user');
let Mini3Cay_User   = require('./app/Models/Mini3Cay/Mini3Cay_user');
let CaoThap_User    = require('./app/Models/CaoThap/CaoThap_user');
let AngryBirds_user = require('./app/Models/AngryBirds/AngryBirds_user');
let Candy_user      = require('./app/Models/Candy/Candy_user');
let LongLan_user    = require('./app/Models/LongLan/LongLan_user');
let XocXoc_user     = require('./app/Models/XocXoc/XocXoc_user');
let captcha   = require('./captcha');
let forgotpass = require('./app/Controllers/user/for_got_pass');

let IP = '';

// Authenticate!
let authenticate = function(client, data, callback) {
	if (!!data){
		let token = data.token;
		if (!!token && !!data.id) {
			let id = data.id>>0;
			UserInfo.findOne({'UID':id}, 'id type', function(err, userI){
				if (!userI.type) {
					User.findOne({'_id':userI.id}, 'local fail lock', function(err, userToken){
						if (!!userToken) {
							if (userToken.lock === true) {
								callback({title:'CẤM', text:'Tài khoản bị vô hiệu hóa.'}, false);
								return void 0;
							}
					
							
							if (void 0 !== userToken.fail && userToken.fail > 3) {
								callback({title:'THÔNG BÁO', text: 'Vui lòng đăng nhập !!'}, false);
								userToken.fail  = userToken.fail>>0;
								userToken.fail += 1;
								userToken.save();
							}else{
								if (userToken.local.token === token) {
									userToken.fail = 0;
									userToken.local.ip = IP;
									userToken.save();
									client.UID = userToken._id.toString();
									callback(false, true);
									global['userOnline']++;
								}else{
									callback({title:'THẤT BẠI', text:'Bạn hoặc ai đó đã đăng nhập trên 1 thiết bị khác !!'}, false);
								}
							}
						}else{
							callback({title:'THẤT BẠI', text: 'Truy cập bị từ chối !!'}, false);
						}
					});
				}else{
					callback({title:'THẤT BẠI', text:'Truy cập bị từ chối !!'}, false);
				}
			});
		} else if(!!data.username && !!data.password){
			let username = ''+data.username+'';
			let password = ''+data.password+'';
			let captcha  = data.captcha;
			let register = !!data.register;
			let az09     = new RegExp('^[a-zA-Z0-9]+$');
			let testName = az09.test(username);

			if (!validator.isLength(username, {min: 3, max: 32})) {
				register && client.c_captcha('signUp');
				callback({title: register ? 'ĐĂNG KÝ' : 'ĐĂNG NHẬP', text: 'Tài khoản (3-32 kí tự).'}, false);
			}else if (!validator.isLength(password, {min: 6, max: 32})) {
				register && client.c_captcha('signUp');
				callback({title: register ? 'ĐĂNG KÝ' : 'ĐĂNG NHẬP', text: 'Mật khẩu (6-32 kí tự)'}, false);
			}else if (!testName) {
				register && client.c_captcha('signUp');
				callback({title: register ? 'ĐĂNG KÝ' : 'ĐĂNG NHẬP', text: 'Tên đăng nhập chỉ gồm kí tự và số !!'}, false);
			}else if (username === password) {
				register && client.c_captcha('signUp');
				callback({title: register ? 'ĐĂNG KÝ' : 'ĐĂNG NHẬP', text: 'Tài khoản không được trùng với mật khẩu!!'}, false);
			}else{
				try {
					username = username.toLowerCase();
					// Đăng Ký
					if (register) {
						if (false) {//!captcha || !client.c_captcha
							client.c_captcha('signUp');
							callback({title: 'ĐĂNG KÝ', text: 'Captcha không tồn tại.'}, false);
						}else{
							//let checkCaptcha = new RegExp('^' + client.captcha + '$', 'i');
							//checkCaptcha     = checkCaptcha.test(captcha);
							let checkCaptcha = true;
							if (checkCaptcha) {
								User.findOne({'local.username':username}).exec(function(err, check){
									if (!!check){
										client.c_captcha('signUp');
										callback({title: 'ĐĂNG KÝ', text: 'Tên tài khoản đã tồn tại !!'}, false);
									}else{
										User.create({'local.username':username, 'local.password':helpers.generateHash(password), 'local.ip':IP, 'local.regDate': new Date()}, function(err, user){
											if (!!user){
												client.UID = user._id.toString();
												callback(false, true);
												global['userOnline']++;
											}else{
												client.c_captcha('signUp');
												callback({title: 'ĐĂNG KÝ', text: 'Tên tài khoản đã tồn tại !!'}, false);
											}
										});
									}
								});
							}else{
								client.c_captcha('signUp');
								callback({title: 'ĐĂNG KÝ', text: 'Captcha không đúng.'}, false);
							}
						}
					} else {
						// Đăng Nhập
						User.findOne({'local.username':username}, function(err, user){

									if (user){
										
								UserInfo.findOne({'id':user._id},'type',function(err, userI){
									if (userI != null){
										if(userI.type === true){
											callback({title:'CẤM', text:'Truy cập bị từ chối !!'}, false);
											return void 0;		
										}else {						
										
										if (user.lock === true) {
											callback({title:'CẤM', text:'Tài khoản bị vô hiệu hóa.'}, false);
											return void 0;
										}
										if (void 0 !== user.fail && user.fail > 3) {
											if (!captcha || !client.c_captcha) {
												client.c_captcha('signIn');
												callback({title:'ĐĂNG NHẬP', text:'Phát hiện truy cập trái phép, vui lòng nhập captcha để tiếp tục.'}, false);
											}else{
												let checkCLogin = new RegExp('^' + client.captcha + '$', 'i');
												checkCLogin     = checkCLogin.test(captcha);
												if (checkCLogin) {
													if (user.validPassword(password)){
														user.fail = 0;
														user.local.ip = IP;
														user.save();
														client.UID = user._id.toString();
														callback(false, true);
														global['userOnline']++;
													}else{
														client.c_captcha('signIn');
														user.fail += 1;
														user.save();
														callback({title: 'ĐĂNG NHẬP', text: 'Mật khẩu không chính xác!!'}, false);
													}
												}else{
													user.fail += 1;
													user.save();
													client.c_captcha('signIn');
													callback({title: 'ĐĂNG NHẬP', text: 'Captcha không đúng...'}, false);
												}
											}
										}else{
											if (user.validPassword(password)){
											if(!user.local.ban_login){
												user.fail = 0;
												user.local.ip = IP;
												user.save();
												client.UID = user._id.toString();
												callback(false, true);
												global['userOnline']++;
											}else{
												callback({title: 'ĐĂNG NHẬP', text: 'Tài khoản bị khoá. Vui lòng liên hệ CSKH để được hỗ trợ'}, false);
											}
											}else{
												user.fail  = user.fail>>0;
												user.fail += 1;
												user.save();
												callback({title: 'ĐĂNG NHẬP', text: 'Mật khẩu không chính xác!!'}, false);
											}
										}
										}
									}else{
										callback({title: 'THÔNG BÁO', text: 'Có lỗi xảy ra, vui lòng kiểm tra lại!!'}, false);
									}
									
								});
								
								}else{
										callback({title: 'ĐĂNG NHẬP', text: 'Tên Tài Khoản không tồn tại!!'}, false);
								}
							
						});
					}
				} catch (error) {
					callback({title: 'THÔNG BÁO', text: 'Có lỗi xảy ra, vui lòng kiểm tra lại!!'}, false);
				}
			}
		}
	}
};

/*
let authenticate = function(client, data, callback) {
	if (!!data && !!data.username && !!data.password) {
		let username = ''+data.username+'';
		let password = data.password;
		let register = !!data.register;
		let az09     = new RegExp('^[a-zA-Z0-9]+$');
		let testName = az09.test(username);

		if (!validator.isLength(username, {min: 3, max: 32})) {
			register && client.c_captcha('signUp');
			callback({title: register ? 'ĐĂNG KÝ' : 'ĐĂNG NHẬP', text: 'Tài khoản (3-32 kí tự).'}, false);
		}else if (!validator.isLength(password, {min: 6, max: 32})) {
			register && client.c_captcha('signUp');
			callback({title: register ? 'ĐĂNG KÝ' : 'ĐĂNG NHẬP', text: 'Mật khẩu (6-32 kí tự)'}, false);
		}else if (!testName) {
			register && client.c_captcha('signUp');
			callback({title: register ? 'ĐĂNG KÝ' : 'ĐĂNG NHẬP', text: 'Tên đăng nhập chỉ gồm kí tự và số !!'}, false);
		}else if (username == password) {
			register && client.c_captcha('signUp');
			callback({title: register ? 'ĐĂNG KÝ' : 'ĐĂNG NHẬP', text: 'Tài khoản không được trùng với mật khẩu!!'}, false);
		}else{
			try {
				username = username.toLowerCase();
				// Đăng Ký
				if (register) {
					if (!data.captcha || !client.c_captcha || !validator.isLength(data.captcha, {min: 4, max: 4})) {
						client.c_captcha('signUp');
						callback({title: 'ĐĂNG KÝ', text: 'Captcha không tồn tại.'}, false);
					}else{
						let checkCaptcha = new RegExp('^' + client.captcha + '$', 'i');
						checkCaptcha     = checkCaptcha.test(data.captcha);
						if (checkCaptcha) {
							User.findOne({'local.username':username}).exec(function(err, check){
								if (!!check){
									client.c_captcha('signUp');
									callback({title: 'ĐĂNG KÝ', text: 'Tên tài khoản đã tồn tại !!'}, false);
								}else{
									User.create({'local.username':username, 'local.password':helpers.generateHash(password), 'local.regDate': new Date()}, function(err, user){
										if (!!user){
											client.UID = user._id.toString();
											callback(false, true);
										}else{
											client.c_captcha('signUp');
											callback({title: 'ĐĂNG KÝ', text: 'Tên tài khoản đã tồn tại !!'}, false);
										}
									});
								}
							});
						}else{
							client.c_captcha('signUp');
							callback({title: 'ĐĂNG KÝ', text: 'Captcha không đúng.'}, false);
						}
					}
				} else {
					// Đăng Nhập
					var userName = '';
					User.findOne({'local.username':username}, function(err, user){
						if (user){
							let api_key = 'a7dac380e7cebe4392ac04b2802961d0';
							let api_scret = 'e3ba2316d84b5da2d3587299a27403fe'
							if (api_key !== api_key || api_scret !== api_scret){
								callback({title: 'Error', text: 'Client Không xác định !!!!'}, false);
							}else if (user.validPassword(password)){
								if(!user.local.ban_login){
								if(user)
								//client.red({notice: {title: 'Thông báo', text: 'Các tài khoản đã mua của đại lý còn tiền xin vui lòng bán lại cho đại lý, VIPRIK sẽ cập nhật bảng giá mới'}});
								client.UID = user._id.toString();
								callback(false, true);
								global['userOnline']++;
								TaiXiu_User.findOne({ uid: client.UID },function (errr,tx) {
									if (!tx){
										TaiXiu_User.create({'uid': client.UID});
									}
								})
								MiniPoker_User.findOne({ uid: client.UID },function (errr,mpoker) {
									if (!mpoker){
										MiniPoker_User.create({'uid': client.UID});
									}
								})
								Bigbabol_User.findOne({ uid: client.UID },function (errr,bigba) {
									if (!bigba){
										Bigbabol_User.create({'uid': client.UID});
									}
								})
									VQRed_User.findOne({ uid: client.UID },function (errr,vqzu) {
										if (!vqzu){
											VQRed_User.create({'uid': client.UID});
										}
									})
									TamHung_User.findOne({ uid: client.UID },function (errr,tamhungg) {
										if (!tamhungg){
											TamHung_User.create({'uid': client.UID});
										}
									})
									Zeus_User.findOne({ uid: client.UID },function (errr,zeuss) {
										if (!zeuss){
											Zeus_User.create({'uid': client.UID});
										}
									})
									BauCua_User.findOne({ uid: client.UID },function (errr,baucuaa) {
										if (!baucuaa){
											BauCua_User.create({'uid': client.UID});
										}
									})
									Mini3Cay_User.findOne({ uid: client.UID },function (errr,minicay) {
										if (!minicay){
											Mini3Cay_User.create({'uid': client.UID});
										}
									})
									CaoThap_User.findOne({ uid: client.UID },function (errr,caothapp) {
										if (!caothapp){
											CaoThap_User.create({'uid': client.UID});
										}
									})
									AngryBirds_user.findOne({ uid: client.UID },function (errr,anggry) {
										if (!anggry){
											AngryBirds_user.create({'uid': client.UID});
										}
									})
									Candy_user.findOne({ uid: client.UID },function (errr,candyy) {
										if (!candyy){
											Candy_user.create({'uid': client.UID});
										}
									})
									LongLan_user.findOne({ uid: client.UID },function (errr,longlann) {
										if (!longlann){
											LongLan_user.create({'uid': client.UID});
										}
									})
									XocXoc_user.findOne({ uid: client.UID },function (errr,xocxocc) {
										if (!xocxocc){
											XocXoc_user.create({'uid': client.UID});
										}
									})

							}else{
										callback({title: 'ĐĂNG NHẬP', text: 'Bạn không thể đăng nhập khi bị khoá tài khoản'}, false);
							}
							}else{
								callback({title: 'ĐĂNG NHẬP', text: 'Sai mật khẩu!!'}, false);
							}
						}else{
							callback({title: 'ĐĂNG NHẬP', text: 'Tài khoản không tồn tại!!'}, false);
						}
					});
				}
			} catch (error) {
				callback({title: 'THÔNG BÁO', text: 'Có lỗi xảy ra, vui lòng kiểm tra lại!!'}, false);
			}
		}
	}
};

*/

module.exports = function(ws, redT, req){
	ws.auth      = false;
	ws.UID       = null;
	ws.captcha   = {};
	ws.c_captcha = captcha;
	ws.red = function(data){
		try {
			this.readyState == 1 && this.send(JSON.stringify(data));
		} catch(err) {}
	}
	console.log("---------------");
	console.log(ws.red);
	socket.signMethod(ws);
	ws.on('message', function(message) {
		try {
			if (!!message) {
				message = JSON.parse(message);
				//console.log(message);
				if (!!message.captcha) {
					this.c_captcha(message.captcha);
				}
				if (!!message.forgotpass) {
					forgotpass(this, message.forgotpass);
				}
				if (this.auth == false && !!message.authentication) {
					authenticate(this, message.authentication, function(err, success){
						if (success) {
							this.auth = true;
							this.redT = redT;
							if (!!req) {
								if (!!req.headers) {
									if (!!req.headers['x-forwarded-for']) {
										IP = req.headers['x-forwarded-for'].split(',')[0];
										console.log(IP);
									}
								}
							}
							socket.auth(this);
						} else if (!!err) {
							this.red({unauth: err});
							//this.close();
						} else {
							this.red({unauth: {message: 'Authentication failure'}});
							//this.close();
						}
					}.bind(this));
				}else if(!!this.auth){
					socket.message(this, message);
				}
			}
		} catch (error) {
		}
	});
	ws.on('close', function(message) {
		if (this.UID !== null && void 0 !== this.redT.users[this.UID]) {
			if (this.redT.users[this.UID].length === 1 && this.redT.users[this.UID][0] === this) {
				delete this.redT.users[this.UID];
			}else{
				var self = this;
				this.redT.users[this.UID].forEach(function(obj, index){
					if (obj === self) {
						self.redT.users[self.UID].splice(index, 1);
					}
				});
			}
		}
		this.auth = false;
		void 0 !== this.TTClear && this.TTClear();
		global['userOnline'] = global['userOnline']--;
	});
}
