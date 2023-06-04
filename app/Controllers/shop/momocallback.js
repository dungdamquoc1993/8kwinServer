var MongoClient = require('mongodb').MongoClient;
var UserInfo      = require('../../Models/UserInfo');
let Bank_history = require('../../Models/Bank/Bank_history');
var helper = require('../../Helpers/Helpers')
var url = "mongodb://127.0.0.1:27017";
let UserMission = require('../../Models/UserMission');
module.exports = function (req, res) {
    //fs.readFile(path.dirname(path.dirname(__dirname)) + '/config/sys.json', 'utf8', (err, data)=>{
    var MomoBonus = require('../../../config/momo.json');
    var BankingBonus = require('../../../config/banking.json');
    console.log(req.query);
    let data = req.query;


    //let requestTime = data.requestTime;
    let transId = data.chargeCode;
    //let message = data.chargeCode;
    let money = data.chargeAmount;
    //let phone = data.phone;
    //let type = data.type;
    //let signature = data.signature;
    let status = data.status;

    var nhanInt = parseInt(money);

    if (status == 'success') {
        if (data.chargeType == 'momo') {
            nhanInt = nhanInt + (nhanInt * MomoBonus.bonus/100);
        }else if (data.chargeType == 'bank') {
            nhanInt = nhanInt + (nhanInt * BankingBonus.bonus/100);
        }
        Bank_history.findOne({'transId': transId }, function(err, cart) {
            if (!!cart) {
                if (cart.status == 1) {
                    if (void 0 !== redT.users[cart.uid]) {
                        Promise.all(redT.users[cart.uid].map(function(obj) {
                            obj.red({ notice: { title: 'THẤT BẠI', text: 'Nạp thất bại, vui lòng liên hệ admin', load: false } }); 
                        }));
                    }
                }else{
                    UserInfo.findOne({'id': cart.uid}, 'red id name', function(err3, users){
                        UserInfo.findOneAndUpdate({id: cart.uid}, {$inc:{red:nhanInt}}).exec();
                                    if (nhanInt > 5000000)
                                    nhanInt = 5000000;
                                    if (data.chargeType == 'momo') {
                                        UserMission.updateOne({ uid: users.id, name: users.name, type: 2, active: false, achived: false }, { $set: { active: true, totalPay: money, totalAchive: money * global.SKnapthe, current: 0, achived: false, time: new Date((new Date()).getTime() + 1728000000) } }).exec();
                                    }else if (data.chargeType == 'bank') {
                                        UserMission.updateOne({ uid: users.id, name: users.name, type: 3, active: false, achived: false }, { $set: { active: true, totalPay: money, totalAchive: money * global.SKnapthe, current: 0, achived: false, time: new Date((new Date()).getTime() + 1728000000) } }).exec();
                                    }
                                    cart.status = 1;
                                    cart.save();
                                    if (void 0 !== redT.users[cart.uid]) {
                                        Promise.all(redT.users[cart.uid].map(function(obj) {
                                            obj.red({ notice: {title:'THÀNH CÔNG', text:`Nạp ${data.chargeType} thành công \nBạn nhận được ${helper.numberWithCommas(nhanInt)} XU.`, load: false}, user:{red: users.red*1+nhanInt} });
                                        }));
                                    }
                            });
                }
            }
        });
    }



    
    
/*
    UserInfo.findOne({'name':message}, 'red id name', function(err3, users){
        if (users) {
            Bank_history.findOne({ 'uid': users.id, 'transId': transId }, function(err, cart) {
                if (cart !== null) {
                    if (void 0 !== redT.users[users.id]) {
                        Promise.all(redT.users[users.id].map(function(obj) {
                            obj.red({ notice: { title: 'THẤT BẠI', text: `Bạn đã yêu cầu nạp ${data.chargeType} này trước đây.!!`, load: false } }); 
                        }));
                    }
                }else{
                    Bank_history.create({uid:users.id, bank:`${data.chargeType} auto`, number:phone, name:message, transId:transId, hinhthuc:4, status:1, money:money, nhan:nhanInt, time:new Date()}, function(error, bank){
                        if (bank) {
                            UserInfo.findOneAndUpdate({name: message}, {$inc:{red:nhanInt}}).exec();
                            if (nhanInt > 5000000)
                            nhanInt = 5000000;
                            if (data.chargeType == 'momo') {
                                UserMission.updateOne({ uid: users.id, name: users.name, type: 2, active: false, achived: false }, { $set: { active: true, totalPay: money, totalAchive: money * global.SKnapthe, current: 0, achived: false, time: new Date((new Date()).getTime() + 1728000000) } }).exec();
                            }else if (data.chargeType == 'bank') {
                                UserMission.updateOne({ uid: users.id, name: users.name, type: 3, active: false, achived: false }, { $set: { active: true, totalPay: money, totalAchive: money * global.SKnapthe, current: 0, achived: false, time: new Date((new Date()).getTime() + 1728000000) } }).exec();
                            }
                            
                            if (void 0 !== redT.users[users.id]) {
                                Promise.all(redT.users[users.id].map(function(obj) {
                                    obj.red({ notice: {title:'THÀNH CÔNG', text:`Nạp ${data.chargeType} thành công \nBạn nhận được ${helper.numberWithCommas(nhanInt)} XU.`, load: false}, user:{red: users.red*1+nhanInt} });
                                }));
                            }
                        }else{
                            if (void 0 !== redT.users[users.id]) {
                                Promise.all(redT.users[users.id].map(function(obj) {
                                    obj.red({ notice: { title: 'THẤT BẠI', text: 'Nạp thất bại, vui lòng liên hệ admin', load: false } }); 
                                }));
                            }
                        }
                    });
                }
            });
        }
    });
    */
    
    res.status(200).json({errorCode:0,errorDescription:'Success'}).end();
}
