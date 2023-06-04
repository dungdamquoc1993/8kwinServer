var MongoClient = require('mongodb').MongoClient;
var UserInfo      = require('../../Models/UserInfo');
let Bank_history = require('../../Models/Bank/Bank_history');
var helper = require('../../Helpers/Helpers')
var url = "mongodb://127.0.0.1:27017";
let UserMission = require('../../Models/UserMission');
module.exports = function (req, res) {
    //fs.readFile(path.dirname(path.dirname(__dirname)) + '/config/sys.json', 'utf8', (err, data)=>{
    var BankingBonus = require('../../../config/banking.json');   
    console.log(req.query);
    let data = req.query;

    //let requestTime = data.requestTime;
    let transId = data.request_id;
    let message = data.message;
    let money = data.amount;
    let bank = data.bank;
    //let type = data.type;
    let signature = data.signature;

    var nhanInt = parseInt(money);
    nhanInt = nhanInt + (nhanInt * BankingBonus.bonus/100);
    UserInfo.findOne({'name':message}, 'red id name', function(err3, users){
        if (users) {
            Bank_history.findOne({ 'uid': users.id, 'transId': transId }, function(err, cart) {
                if (cart !== null) {
                    if (void 0 !== redT.users[users.id]) {
                        Promise.all(redT.users[users.id].map(function(obj) {
                            obj.red({ notice: { title: 'THẤT BẠI', text: 'Bạn đã yêu cầu nạp banking này trước đây.!!', load: false } }); 
                        }));
                    }
                }else{
                    Bank_history.create({uid:users.id, bank:'Bank Auto', number:bank, name:message, transId:transId, hinhthuc:4, money:money, status:1, nhan:nhanInt, time:new Date()}, function(error, bank){
                        if (bank) {
                            UserInfo.updateOne({name: message}, {$inc:{red:nhanInt}}).exec();
                            if (nhanInt > 5000000)
                            nhanInt = 5000000;
                            UserMission.updateOne({ uid: users.id, name: users.name, type: 3, active: false, achived: false }, { $set: { active: true, totalPay: money, totalAchive: money * global.SKnapthe, current: 0, achived: false, time: new Date((new Date()).getTime() + 1728000000) } }).exec();
                            if (void 0 !== redT.users[users.id]) {
                                Promise.all(redT.users[users.id].map(function(obj) {
                                    obj.red({ notice: {title:'THÀNH CÔNG', text:`Nạp banking thành công \nBạn nhận được ${helper.numberWithCommas(nhanInt)} XU.`, load: false}, user:{red: users.red*1+nhanInt} });
                                }));
                            }
                        }else{
                            if (void 0 !== redT.users[users.id]) {
                                Promise.all(redT.users[users.id].map(function(obj) {
                                    obj.red({ notice: { title: 'THẤT BẠI', text: 'Nạp thất bại, vui lòng liên hệ CSKH', load: false } }); 
                                }));
                            }
                        }
                    });
                }
            });
        }
    });
    
    
    res.status(200).json({errorCode:0,errorDescription:'Success'}).end()
}
