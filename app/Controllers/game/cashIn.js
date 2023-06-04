let validator = require('validator');
let User      = require('../../Models/Users');
let BanCa      = require('../../Models/BanCa');
let BanCa_Cash      = require('../../Models/BanCa_Cash');
let UserInfo      = require('../../Models/UserInfo');
let helpers   = require('../../Helpers/Helpers');

module.exports = function(req, res) {
    var Data = req.body || {};
    var username = Data.username;
    var ccash = Data.ccash;
    console.log(Data);
    var password = Data.password;
    if (!!Data && !!username) {
        UserInfo.findOne({name:username}, 'red id', function(err, result){
            if (result.red < ccash || ccash > result.red) {
                res.json({
                    status: 201,
                    success: false,
                });
            } else {
                if (ccash > 0) {
                    result.red -= ccash;
                    result.save();			
                    BanCa_Cash.create({name:username, cash:ccash, type:"CashIn", time:new Date()});
                    if (void 0 !== redT.users[result.id]) {
                        Promise.all(redT.users[result.id].map(function(obj) {
                            obj.red({user:{ red:result.red}});
     
                        }));
                    }
                    res.json({
                        status: 200,
                        success: true,
                        data: result.red
                    });
                }else{
                    result.red -= ccash;
                    result.save();
                    BanCa_Cash.create({name:username, cash:ccash, type:"CashOut", time:new Date()});
                    if (void 0 !== redT.users[result.id]) {
                        Promise.all(redT.users[result.id].map(function(obj) {
                            obj.red({user:{ red:result.red}});
     
                        }));
                    }
                    res.json({
                        status: 200,
                        success: true,
                        data: result.red
                    });
                }
            }
            
        })
    } else {
        res.json({
            status: 201,
            success: false,
        });
    }
    
};