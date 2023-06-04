
let TXCuoc    = require('../../Models/TaiXiu_cuoc');
let TXCuocOne = require('../../Models/TaiXiu_one');
let TXChat = require('../../Models/TaiXiu_chat');
let TXBotChat = require('../../Models/TaiXiu_bot_chat');
let User      = require('../../Models/Users');
let UserInfo = require('../../Models/UserInfo');
let helpers   = require('../../Helpers/Helpers');
var validator   = require('validator');
var shortid   = require('shortid');
var fs 			= require('fs');

// Game User
let TaiXiu_User     = require('../../Models/TaiXiu_user');
let MiniPoker_User  = require('../../Models/miniPoker/miniPoker_users');
let Bigbabol_User   = require('../../Models/BigBabol/BigBabol_users');
let VQRed_User      = require('../../Models/VuongQuocRed/VuongQuocRed_users');
let BauCua_User     = require('../../Models/BauCua/BauCua_user');
let Mini3Cay_User   = require('../../Models/Mini3Cay/Mini3Cay_user');
let CaoThap_User    = require('../../Models/CaoThap/CaoThap_user');
let AngryBirds_user = require('../../Models/AngryBirds/AngryBirds_user');
let Candy_user      = require('../../Models/Candy/Candy_user');
let LongLan_user    = require('../../Models/LongLan/LongLan_user');
let XocXoc_user     = require('../../Models/XocXoc/XocXoc_user');
let TamHung_User    = require('../../Models/TamHung/TamHung_users');
let Zeus_User     = require('../../Models/Zeus/Zeus_user');
let MegaJP_user     = require('../../Models/MegaJP/MegaJP_user');

/**
 * Ngẫu nhiên cược
 * return {number}
*/
let random1 = function(){
	let a = (Math.random()*35)>>0;
	if (a == 34) {
		  // 34
		  return (Math.floor(Math.random()*(50-20+1))+20)*1000000;
	  }else{
		return (Math.floor(Math.random()*(100-30+1))+30)*100000;
	}
};

let random2 = function(){
	let a = (Math.random()*40)>>0;
	if (a == 34) {
		// 34
		return (Math.floor(Math.random()*(50-20+1))+20)*100000;
	}else if (a >= 32 && a < 34) {
		// 32 33
		return (Math.floor(Math.random()*(30-20+1))+20)*100000;
	}else if (a >= 41 && a < 51) {
		// 41 - 44
		return (Math.floor(Math.random()*(50-20+1))+20)*100000;
	}else if (a >= 26 && a < 32) {
		// 30 31 32
		return (Math.floor(Math.random()*(20-10+1))+10)*100000;
	}else if (a >= 19 && a < 25) {
		// 26 27 28 29
		return (Math.floor(Math.random()*(15-5+1))+5)*1000000;
	}else if (a >= 14 && a < 18) {
		// 21 22 23 24 25
		return (Math.floor(Math.random()*(15-3+1))+3)*100000;
	}else if (a >= 9 && a < 13) {
		// 15 16 17 18 19 20
		return (Math.floor(Math.random()*(100-30+1))+30)*100000;
	}else if (a >= 4 && a < 8) {
		// 8 9 10 11 12 13 14
		return (Math.floor(Math.random()*(50-20+1))+20)*100000;
	}else if (a >= 35 && a < 41) {
		// 8 9 10 11 12 13 14
		return (Math.floor(Math.random()*(30-20+1))+10)*10000;
	}else{
		// 0 1 2 3 4 5 6 7
		return (Math.floor(Math.random()*(30-20+1))+20)*100000;
	}
};
  
let modeValueMin = [
	1000,
	20000,
	50000
]; // 3 mốc thấp nhất
let modeValueMax = [
	50000,
	200000,
	1000000
]; // 3 mốc cao nhất

let random = function(){
	var BotMode = require('../../../config/taixiubotmode.json');
	return Math.round(((Math.random()*(modeValueMax[BotMode.bot]-modeValueMin[BotMode.bot]+1)+modeValueMin[BotMode.bot])>>0)/1000)*1000;
};

/**
 * Cược
*/
// Tài Xỉu RED
let tx = function(bot, io){
	
	let cuoc   = random2();
	//cuoc = 2 * cuoc / 4;
	//let cuoc = (Math.floor(Math.random()*(10-20+1))+10)*10000;
	let select = !!((Math.random()*2)>>0);
	if (select) {
		io.taixiu.taixiu.red_tai        += cuoc;
		io.taixiu.taixiu.red_player_tai += 9;
	}else{
		io.taixiu.taixiu.red_xiu        += cuoc;
		io.taixiu.taixiu.red_player_xiu += 9;
	}
	TXCuocOne.create({uid:bot.id, phien:io.TaiXiu_phien, taixiu:true, select:select, red:true, bet:cuoc});
	TXCuoc.create({uid:bot.id, name:bot.name, phien:io.TaiXiu_phien, bet:cuoc, taixiu:true, select:select, red:true, time:new Date()});
	bot = null;
	io = null;
	cuoc   = null;
	select = null;
};

// Chẵn Lẻ RED
let cl = function(bot, io){
	let cuoc   = random1();
	let select = !!((Math.random()*2)>>0);
	if (select) {
		io.taixiu.chanle.red_chan        += cuoc;
		io.taixiu.chanle.red_player_chan += 1;
	}else{
		io.taixiu.chanle.red_le        += cuoc;
		io.taixiu.chanle.red_player_le += 1;
	}
	TXCuocOne.create({uid:bot.id, phien:io.TaiXiu_phien, taixiu:false, select:select, red:true, bet:cuoc});
	TXCuoc.create({uid:bot.id, name:bot.name, phien:io.TaiXiu_phien, bet:cuoc, taixiu:false, select:select, red:true, time:new Date()});
	bot = null;
	io = null;
	cuoc   = null;
	select = null;
};

let regbot = function(){
	var username = 'nohu' + helpers.RandomUserName(5) + helpers.RandomUserName(1);
	var name = 'nohu' + helpers.RandomUserName(1) + helpers.RandomUserName(2) + helpers.RandomUserName(3);
	User.create({'local.username':username, 'local.password':helpers.generateHash(username), 'local.regDate': new Date()}, function(err, user){
		if (!!user){
			var bot_uid = user._id.toString();
			UserInfo.create({'id':bot_uid, 'name':name, 'type':true, 'joinedOn':new Date()}, function(errC, userB){
				if (!!errC) {
					console.log('reg fail name: '+ name);
				}else{
					userB = userB._doc;
					userB.level   = 1;
					userB.vipNext = 100;
					userB.vipHT   = 0;
					userB.phone   = '';

					delete userB._id;
					delete userB.redWin;
					delete userB.redLost;
					delete userB.redPlay;
					delete userB.xuWin;
					delete userB.xuLost;
					delete userB.xuPlay;
					delete userB.thuong;
					delete userB.vip;
					delete userB.hu;
					delete userB.huXu;
					
					TaiXiu_User.create({'uid': bot_uid});
					MiniPoker_User.create({'uid': bot_uid});
					Bigbabol_User.create({'uid': bot_uid});
					VQRed_User.create({'uid': bot_uid});
					BauCua_User.create({'uid': bot_uid});
					Mini3Cay_User.create({'uid': bot_uid});
					CaoThap_User.create({'uid': bot_uid});
					AngryBirds_user.create({'uid': bot_uid});
					Candy_user.create({'uid': bot_uid});
					LongLan_user.create({'uid': bot_uid});
					TamHung_User.create({'uid': bot_uid});
					Zeus_User.create({'uid': bot_uid});
					
					XocXoc_user.create({'uid': bot_uid});
					MegaJP_user.create({'uid': bot_uid});

					TXBotChat.create({'Content': bot_uid});

					console.log('reg suss name: '+ name);

				}
			});
			console.log('reg suss acc: '+ username);
		}else{
			console.log('reg fail acc: '+ username);
		}
	});
	
};

module.exports = {
	tx: tx,
	cl: cl,
	regbot: regbot,
}
