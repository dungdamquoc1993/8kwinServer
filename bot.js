// Setting & Connect to the Database
let configDB = require('./config/database');
let mongoose = require('mongoose');
let User      = require('./app/Models/Users');

let helpers   = require('./app/Helpers/Helpers');
// mongoose.set('debug', true);
require('mongoose-long')(mongoose); // INT 64bit

mongoose.set('useFindAndModify', false);
mongoose.set('useCreateIndex', true);

console.log(configDB);

mongoose.connect(configDB.url, configDB.options)
    .then(function () {
        // console.log('Connect to MongoDB success');

         
         
		let TaiXiu_User     = require('./app/Models/TaiXiu_user');
		let MiniPoker_User  = require('./app/Models/miniPoker/miniPoker_users');
		let Bigbabol_User   = require('./app/Models/BigBabol/BigBabol_users');
		let VQRed_User      = require('./app/Models/VuongQuocRed/VuongQuocRed_users');
		let BauCua_User     = require('./app/Models/BauCua/BauCua_user');
		let Mini3Cay_User   = require('./app/Models/Mini3Cay/Mini3Cay_user');
		let CaoThap_User    = require('./app/Models/CaoThap/CaoThap_user');
		let AngryBirds_user = require('./app/Models/AngryBirds/AngryBirds_user');
		let Candy_user      = require('./app/Models/Candy/Candy_user');
		let LongLan_user    = require('./app/Models/LongLan/LongLan_user');
	 
		let XocXoc_user     = require('./app/Models/XocXoc/XocXoc_user');
		let MegaJP_user     = require('./app/Models/MegaJP/MegaJP_user');
	 
        let UserInfo    = require('./app/Models/UserInfo');
		 
        let password = "@demo12345";
        var fs = require('fs');
        var obj = JSON.parse(fs.readFileSync('config/bot-username.json', 'utf8'));

        var crypto = require('crypto');

        let az09     = new RegExp('^[a-zA-Z0-9]+$');
        let users = [];

        for(let i in obj){
            let testName = az09.test(obj[i]);
            if(testName && obj[i].length <= 14){
                users.push(obj[i]);
            }
        }
        function randomDate(start, end) {
            return new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
        }

        function InsertUserInfo(Uid,name,date_create) {
            UserInfo.findOne({'name':name}, 'name', function(err, check){
                if (!!check) {

                }else{
                    try {
                        UserInfo.create({'id':Uid, 'name':name, 'joinedOn':date_create,'type':true}, function(errC, user){
                            if (!!errC) {

                            }else{
                                user = user._doc;
                                user.level   = 1;
                                user.vipNext = 100;
                                user.vipHT   = 0;
                                user.phone   = '';

                                delete user._id;
                                delete user.redWin;
                                delete user.redLost;
                                delete user.redPlay;
                                delete user.xuWin;
                                delete user.xuLost;
                                delete user.xuPlay;
                                delete user.thuong;
                                delete user.vip;
                                delete user.hu;
                                delete user.huXu;
 
 
											 
												TaiXiu_User.create({'uid':Uid});
												MiniPoker_User.create({'uid':Uid});
												Bigbabol_User.create({'uid':Uid});
												VQRed_User.create({'uid':Uid});
												BauCua_User.create({'uid':Uid});
												Mini3Cay_User.create({'uid':Uid});
												CaoThap_User.create({'uid':Uid});
												AngryBirds_user.create({'uid':Uid});
												Candy_user.create({'uid':Uid});
												LongLan_user.create({'uid':Uid});
											 
												XocXoc_user.create({'uid':Uid});
												MegaJP_user.create({'uid':Uid});
												    



                            }
                        });
                    } catch (error) {
                        console.log(error);
                    }
                }
            });

        }


        for(let i in users){
            let name = users[i];
            let username = name+"_bot";
            let date_create = randomDate(new Date(2021, 0, 1), new Date());
            console.log(name);

            User.findOne({'local.username':username}).exec(function(err, check){

                if (!!check){
                    console.log(i+" OKE:"+check._doc._id+ name + check._doc.local.regDate);
                    InsertUserInfo(check._doc._id,name,check._doc.local.regDate);
                }else{

                    User.create({'local.username':username, 'local.password':helpers.generateHash(password), 'local.regDate': date_create}, function(err, user){

                        console.log(i+ "INSERT:"+user._doc._id+ name + user._doc.local.regDate);
                        InsertUserInfo(user._doc._id,name,user._doc.local.regDate);
                    });
                }
            });
        }
        console.log('End');
    })
    .catch(function(error) {
        console.log(error);
        if (error)
            console.log('Connect to MongoDB failed', error);
        else{

        }
});

