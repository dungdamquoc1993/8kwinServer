let CronJob = require('cron').CronJob;
var UserInfo    = require('../app/Models/UserInfo');
var TaiXiu_User  = require('../app/Models/TaiXiu_user');
var MiniPoker_User  = require('../app/Models/miniPoker/miniPoker_users');
var Bigbabol_User  = require('../app/Models/BigBabol/BigBabol_users');
var VQRed_User  = require('../app/Models/VuongQuocRed/VuongQuocRed_users');
var BauCua_User  = require('../app/Models/BauCua/BauCua_user');
var Mini3Cay_User  = require('../app/Models/Mini3Cay/Mini3Cay_user');
var CaoThap_User  = require('../app/Models/CaoThap/CaoThap_user');
var AngryBirds_user  = require('../app/Models/AngryBirds/AngryBirds_user');
var Candy_user  = require('../app/Models/Candy/Candy_user');
var LongLan_user  = require('../app/Models/LongLan/LongLan_user');
var Zeus_user  = require("../app/Models/Zeus/Zeus_user");
var XocXoc_user  = require('../app/Models/XocXoc/XocXoc_user');
var RongHo_user  = require('../app/Models/RongHo/RongHo_user');
var MegaJP_user  = require('../app/Models/MegaJP/MegaJP_user');
let User      = require('../app/Models/Users');
let helpers   = require('../app/Helpers/Helpers');

module.exports = function() {
	var flag=true;
	new CronJob('*/1011 * * * * *', function () {
		if (flag){
			console.log("------------------START CREATE BOOT GAME-------------------");
			flag=false;
			var lineReader = require('readline').createInterface({
				input: require('fs').createReadStream('./data/nameboot.txt')
				  });
				  lineReader.on('line', function (line) {
						console.log("bat dau tao bot si name =  "+line);
						let username = line+"1";
						let nameacount = line;
						let password= "@laolentop20221";
						//username = username + i ;
						console.log("ten dang nhap là+"+username+" mk la: "+password)
						User.create({'local.username':username, 'local.password':helpers.generateHash(password), 'local.regDate': new Date()}, function(err, user){
						if (!!user){
						let UID=user._id.toString();
						UserInfo.create({'id':UID, 'name':nameacount, 'joinedOn':new Date(), 'type': true ,'red': 10000000}, function(errC, user){
							if (!!errC) {
								client.red({notice:{load: 0, title: 'LỖI', text: 'Tên nhân vật đã tồn tại.'}});
							}else{
										// Tạo token mới
										let txtTH = new Date()+'';
										let token = helpers.generateHash(txtTH);
										//base.local.token = token;
										//base.save();
									user = user._doc;
									user.level   = 1;
									user.vipNext = 100;
									user.vipHT   = 0;
									user.phone   = '';
									user.token   = token;
									delete user._id;
									delete user.redWin;
									delete user.redLost;
									delete user.redPlay;
									delete user.vip;
									delete user.hu;
									delete user.totall;
									delete user.type;
									delete user.otpFirst;
									delete user.gitCode;
									delete user.gitRed;
									delete user.veryold;
									
									//client.profile = {name: user.name, avatar:'0'};
									TaiXiu_User.create({'uid': UID});
									MiniPoker_User.create({'uid': UID});
									Bigbabol_User.create({'uid': UID});
									VQRed_User.create({'uid': UID});
									BauCua_User.create({'uid': UID});
									Mini3Cay_User.create({'uid': UID});
									CaoThap_User.create({'uid': UID});
									AngryBirds_user.create({'uid': UID});
									Candy_user.create({'uid': UID});
									LongLan_user.create({'uid': UID});
									Zeus_user.create({'uid': UID});
									XocXoc_user.create({'uid': UID});
									RongHo_user.create({'uid': UID});
									MegaJP_user.create({'uid': UID});
									
								}	
					
									});
							}else{
								console.log("tai khoan da ton tai stt");
							}
							});
							
			 				
						  });

						  console.log("------------------DONE CREATE BOOT GAME-------------------");
		}

	}, null, true, 'Asia/Ho_Chi_Minh');

}