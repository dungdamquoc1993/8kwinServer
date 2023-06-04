var GiftCode = require('../../../../Models/GiftCode');
var DaiLy = require('../../../../Models/DaiLy');
var Helper = require('../../../../Helpers/Helpers');
var OTP = require('../../../../Models/OTP');
module.exports = function (req, res) {
    const { body, userAuth } = req || {}
    const { Data } = body || {}
    var { menhgia, soluong, ngaythang, event } = Data || {};
    var voucher_codes = require('voucher-code-generator');
    if (menhgia <= 500000) {
        DaiLy.findOne({ nickname: userAuth.nickname }, function (err, resultDl) {
            if (!!resultDl && (menhgia * soluong <= resultDl.giftcodeBank * 1)) {
                var rawData = [];
                var code = voucher_codes.generate({
                    length: 12,
                    count: soluong,
                    charset: "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                });
                for (var i = 0; i < soluong; i++) {
                    rawData.push({
                        'create': userAuth.nickname,
                        'code': code[i],
                        'red': menhgia,
                        'type': event,
                        'date': new Date(),
                        'todate': new Date(ngaythang),
                        'forAgent': userAuth.nickname,
                        'uid': null
                    });
                }

                GiftCode.insertMany(rawData)
                    .then(function (mongooseDocuments) {

                        resultDl.giftcodeBank -= menhgia * soluong;
                        resultDl.save();
                        res.json({
                            status: 200,
                            success: true,
                            data: {
                                message: `Xuất ${soluong} GiftCode Mệnh giá ${Helper._formatMoneyVND(menhgia)} thành công.`
                            }
                        })
                    })
                    .catch(function (err) {
                        res.json({
                            status: 200,
                            success: false,
                            data: {
                                message: 'Tạo GiftCode thất bại.'
                            }
                        })
                    });
            }else{
                res.json({
                    status: 200,
                    success: false,
                    data: {
                        message: 'Số dư tạo code của bạn không đủ'
                    }
                })
            }
        });
    } else {
        res.json({
            status: 200,
            success: false,
            data: {
                message: 'Tạo GiftCode thất bại.'
            }
        })
    }


}
