
let DataNhiemVu    = require('../../../../data/nhiemvu');
var UserInfo = require('../../../Models/UserInfo');
var TX_User  = require('../../../Models/TaiXiu_user');

module.exports = function(client, type) {
    type = type>>0;
    if (type != 0) {
        if (type == 100) {
            TX_User.findOne({'uid': client.UID}, 'tRedPlay tWinRed tLostRed', function(err2, result){
                if (!!result) {
                    var data = new Object();
                    data.nhiemvu = new Object();
                    data.nhiemvu.top = [];
                    let dataNhiemVu = DataNhiemVu[type];
                    data.nhiemvu = dataNhiemVu;
                    var userInfo = new Object();
                    userInfo.tRedPlay = result.tRedPlay/1>>0;
                    userInfo.tWinRed = result.tWinRed/1>>0;
                    userInfo.tLostRed = result.tLostRed/1>>0;
                    client.red({nhiemvu:{userInfo:userInfo, dataNhiemVu}});
                }
            });
        }
    }else{
        client.red({notice:{title:'LỖI', text:'Dữ liệu không đúng!'}});
    }
}