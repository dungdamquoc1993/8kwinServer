let tab_NapThe = require('../../Models/NapThe');
var UserInfo      = require('../../Models/UserInfo');
var helper = require('../../Helpers/Helpers')
var MongoClient = require('mongodb').MongoClient;
var url = "mongodb://127.0.0.1:27017";
let UserMission = require('../../Models/UserMission');
module.exports = function (req, res) {
    var Data = req.body || {};
    console.log(Data);
    let nhan = Data.real_amount;
    let message = Data.message;
    var nhanInt = parseInt(nhan);
    nhanInt = nhanInt;
    let status = Data.status;
    let requestId = Data.request_id;
    let clientUID = '';
    console.log("Server the tra ve " + requestId + " trang thai " + status + " thuc nhan duoc " + nhan);
    if (status == 0) {
        
        tab_NapThe.updateOne({ 'requestId': requestId }, { $set: { nhan: nhanInt, status: 1, time:new Date() } }).exec();
        tab_NapThe.findOne({ 'requestId': requestId }, function (err, result) {
            if (err) throw err;
            if (result != null) {
                console.log(result.uid);
                clientUID = result.uid;
                UserInfo.findOneAndUpdate({'id':result.uid}, {$inc:{red:nhanInt}}, function(err2, user) {
                    if (nhanInt > 5000000)
                    nhanInt = 5000000;
                    UserMission.updateOne({ uid: user.id, name: user.name, type: 1, active: false, achived: false }, { $set: { active: true, totalPay: nhan, totalAchive: nhan * global.SKnapthe, current: 0, achived: false, time: new Date((new Date()).getTime() + 1728000000) } }).exec();
                    if (void 0 !== redT.users[result.uid]) {
                        Promise.all(redT.users[result.uid].map(function(obj) {
                            obj.red({ notice: {title:'THÀNH CÔNG', text:`Nạp thẻ thành công \nBạn nhận được ${helper.numberWithCommas(nhanInt)} XU.`, load: false}, user:{red: user.red*1+nhanInt} });
                        
                        }));
                    }
                    
                });
            }
        });
    }
    else {
        tab_NapThe.findOne({ 'requestId': requestId }, function (err, result) {
            if (err) throw err;
            if (result != null) {
                clientUID = result.uid;
                if (void 0 !== redT.users[result.uid]) {
                    Promise.all(redT.users[result.uid].map(function(obj) {
                        obj.red({ notice: {title:'Thất bại', text:message, load: false}});
                    }));
                }
            }
        });
        tab_NapThe.updateOne({ 'requestId': requestId }, { $set: { nhan: 0, status: 2 } }).exec();
    }
    res.sendStatus(200);
}