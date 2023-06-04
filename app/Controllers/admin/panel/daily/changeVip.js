var DaiLy = require('../../../../Models/DaiLy');
var UserInfo = require('../../../../Models/UserInfo');
var VIPServices = require('../../../../Models/VIPServices');
var Helpers = require('../../../../Helpers/Helpers')
module.exports = function (req, res) {
    const { body, userAuth } = req || {};
    const { Data: data } = body || {};
    DaiLy.findOne({
        nickname: userAuth.nickname
    }, function (err, result) {
        var dangCo = result.vip;
        var doiLanTruoc = result.lastVip;
        var coTheDoi = dangCo - doiLanTruoc;
        if (data.toChange <= coTheDoi) {
            var quyRaDb = data.toChange;
            var quyRaRIK = data.toChange * global.VIPToRIK;
            UserInfo.findOneAndUpdate({ name: userAuth.nickname }, { $inc: { red: quyRaRIK } }, function (err, user) {
                if (!!user) {
                    DaiLy.findOneAndUpdate({ nickname: userAuth.nickname }, { $inc: { lastVip: quyRaDb } }, { new: true, }, function (err, daily) {
                        if (!!daily) {
                            VIPServices.create({ name: userAuth.nickname, reason: "Trả thưởng điểm VIP [" + quyRaRIK + "] RIK", type: false, total: data.toChange, vipFirst: daily.vip, vipLast: daily.vip, time: new Date() }, function (err, small) {
                                //console.log(err);
                                res.json({
                                    status: 200,
                                    success: true,
                                    data: {
                                        message: "Đổi điểm thành công",
                                        vip: daily ? daily.vip >> 0 : 0,
                                        lastVip: daily ? daily.lastVip >> 0 : 0,
                                    }
                                });
                            })
                        } else {
                            res.json({
                                status: 200,
                                success: false,
                                data: {
                                    message: "Vui lòng thử lại"
                                }
                            });
                        }
                    })
                } else {
                    res.json({
                        status: 200,
                        success: false,
                        data: {
                            message: "Vui lòng thử lại"
                        }
                    });
                }
            })

        } else {
            res.json({
                status: 200,
                success: false,
                data: {
                    message: "Bạn không đủ điểm để đổi"
                }
            });
        }
    });
};