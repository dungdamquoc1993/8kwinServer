let UserMission = require('../../Models/UserMission');
let UserInfo = require('../../Models/UserInfo');
let Messages = require('../../Models/Message');
let Helpers = require('../../Helpers/Helpers');
let getdatamission = function (client) {
    UserMission.find({ uid: client.UID, name: client.profile.name }, function (err, result) {
        if (!!result) {
            result.sort(function(a, b){return a.type - b.type});
            Promise.all(result.map(function (obj) {
                obj = obj._doc;
                delete obj._id;
                delete obj.__v;
                delete obj.uid;
                delete obj.name;
                return obj;
            })).then(arrMission => {
                client.red({ user: { mission: arrMission } });
            })
        }
    })
}
let achivement = function (client, data) {
    var type = data >> 0;
    UserMission.findOne({ uid: client.UID, name: client.profile.name, type: type }, function (err, result) {
        if (!result.active) {
            client.red({ notice: { title: 'ĐỔI THƯỞNG', text: 'Bạn chưa tham gia hoạt động này !!!' } })
        } else {
            let today = new Date();
            if(today > result.time){
                client.red({ notice: { title: 'ĐỔI THƯỞNG', text: 'Rất tiếc hoạt động đã quá hạn !!!' } })
            }else{
                if (result.current * 1 >= result.totalPay) {
                    if (result.achived2) {
                        client.red({ notice: { title: 'ĐỔI THƯỞNG', text: 'Bạn đã nhận thưởng hoạt động này rồi !!!' } });
                    } else if (result.achived) {
                        UserInfo.findOneAndUpdate({ id: result.uid }, { $inc: { red: result.totalAchive * 0.5 >> 0 } }, function (err, user) {
                            UserMission.updateOne({ uid: client.UID, name: client.profile.name, type: type }, { $set: { achived2: true,achived: true } }).exec();
                            Messages.create({ uid: user.id, title: "ĐỔI THƯỞNG", text: "Bạn nhận được " + Helpers.numberWithCommas((result.totalAchive * 0.5) >> 0) + " XU từ hoạt động nhân đôi nạp thẻ", time: new Date() });
                            client.red({ notice: { title: 'ĐỔI THƯỞNG', text: 'Đổi thưởng thành công bạn nhận được ' + ((result.totalAchive * 0.5) >> 0) + ' XU !!!' }, user: { red: user.red * 1 + (result.totalAchive * 0.6 >> 0) } });
                        })
                    } else {
                        UserInfo.findOneAndUpdate({ id: result.uid }, { $inc: { red: result.totalAchive * 1 } }, function (err, user) {
                            UserMission.updateOne({ uid: client.UID, name: client.profile.name, type: type }, { $set: { achived2: true, achived: true } }).exec();
                            Messages.create({ uid: user.id, title: "ĐỔI THƯỞNG", text: "Bạn nhận được " + Helpers.numberWithCommas(result.totalAchive * 1) + " XU từ hoạt động nhân đôi nạp thẻ", time: new Date() });
                            client.red({ notice: { title: 'ĐỔI THƯỞNG', text: 'Đổi thưởng thành công bạn nhận được ' + Helpers.numberWithCommas(result.totalAchive * 1) + ' XU !!!' }, user: { red: user.red * 1 + result.totalAchive * 1 } });
                        })
                    }
                } else if (result.current * 1 < result.totalPay && result.current * 1 >=  result.totalPay * 0.5) {
                    if (result.achived) {
                        client.red({ notice: { title: 'ĐỔI THƯỞNG', text: 'Bạn đã nhận thưởng 50% hoạt động này rồi !!!' } })
                    } else {
                        UserInfo.findOneAndUpdate({ id: result.uid }, { $inc: { red: result.totalAchive * 0.5 >> 0 } }, function (err, user) {
                            UserMission.updateOne({ uid: client.UID, name: client.profile.name, type: type }, { $set: { achived: true } }).exec();
                            Messages.create({ uid: user.id, title: "ĐỔI THƯỞNG", text: "Bạn nhận được " + Helpers.numberWithCommas((result.totalAchive * 0.5) >> 0) + " XU từ hoạt động nhân đôi nạp thẻ", time: new Date() });
                            client.red({ notice: { title: 'ĐỔI THƯỞNG', text: 'Đổi thưởng thành công bạn nhận được ' + Helpers.numberWithCommas((result.totalAchive * 0.5) >> 0) + ' XU !!!' }, user: { red: user.red * 1 + (result.totalAchive * 0.5 >> 0) } });
                        })
                    }
                } else {
                    client.red({ notice: { title: 'ĐỔI THƯỞNG', text: 'Bạn chưa đủ điều kiện để nhận thưởng gia hoạt động này !!!' } })
                }
            }
            
        }

    })
}
module.exports = function (client, data) {
    if (!!data.getdata) {
        getdatamission(client);
    }
    if (!!data.nhanthuong) {
        achivement(client, data.nhanthuong);
    }
}