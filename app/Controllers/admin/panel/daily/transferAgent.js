var ChuyenRed = require('../../../../Models/ChuyenRed');
var UserInfo = require('../../../../Models/UserInfo');
var tab_DaiLy = require('../../../../Models/DaiLy');
var OTP = require('../../../../Models/OTP');
var Phone = require('../../../../Models/Phone');
let telegram = require('../../../../Models/Telegram');
let UserMission = require('../../../../Models/UserMission');
var validator = require('validator');
var Helper = require('../../../../Helpers/Helpers');
var nTmux = require('node-tmux');
var fs = require('fs');

module.exports = function (req, res) {

    const { body, userAuth } = req || {}
    const { Data: data } = body || {};
    console.log(userAuth);
    data.name = data.nickname;
    data.red = data.valueRed;
    if (!!data && !!data.name && !!data.otp) {
        if (!validator.isLength(data.name, { min: 3, max: 17 })) {
            res.json({
                status: 200,
                success: false,
                data: {
                    nickname: 'Nickname không hợp lệ.'
                }
            });
        } else if (!validator.isLength(data.otp, { min: 4, max: 6 })) {
            res.json({
                status: 200,
                success: false,
                data: {
                    otp: 'Mã OTP không hợp lệ.'
                }
            });
        } else {
            var red = data.red >> 0;
            var name = '' + data.name + '';
            var otp = data.otp;

            if (validator.isEmpty(name) ||
                red < 10000 ||
                name.length > 17 ||
                name.length < 3 ||
                otp.length != 4) {
                res.json({
                    status: 200,
                    success: false,
                    data: {
                        message: 'Kiểm tra lại các thông tin.'
                    }
                });
            } else {
                Phone.findOne({ 'uid': userAuth.id }, {}, function (err, check) {
                    check = true;
                    if (check) {
                        OTP.findOne({ 'uid': userAuth.id, 'phone': check.phone }, {}, { sort: { '_id': -1 } }, function (err, data_otp) {

                            if (true) {//data_otp && data.otp == data_otp.code
                                if (false) {//((new Date() - Date.parse(data_otp.date)) / 1000) > 180 || data_otp.active

                                    res.json({
                                        status: 200,
                                        success: false,
                                        data: {
                                            otp: 'Mã OTP đã hết hạn.'
                                        }
                                    });
                                } else {
                                    name = name.toLowerCase();
                                    var active1 = tab_DaiLy.findOne({
                                        $or: [
                                            { nickname: name },
                                            { nickname: userAuth.nickname }
                                        ]
                                    }).exec();

                                    var active2 = UserInfo.findOne({ name: name }, 'id name red').exec();
                                    var active3 = UserInfo.findOne({ id: userAuth.id }, 'red').exec();
                                    Promise.all([active1, active2, active3])
                                        .then(valuesCheck => {
                                            var daily = valuesCheck[0];
                                            var to = valuesCheck[1];
                                            var user = valuesCheck[2];
                                            if (!!to) {
                                                if (to.id == userAuth.id) {
                                                    res.json({
                                                        status: 200,
                                                        success: false,
                                                        data: {
                                                            nickname: 'Bạn không thể chuyển cho chính mình.'
                                                        }
                                                    });
                                                } else {
                                                    if (user == null || (user.red - 10000 < red)) {
                                                        res.json({
                                                            status: 200,
                                                            success: false,
                                                            data: {
                                                                message: 'Số dư không khả dụng.'
                                                            }
                                                        });
                                                    } else {

                                                        telegram.findOne({ 'phone': check.phone }, 'form', function (err3, teleCheck) {
                                                            if (!!teleCheck) {
                                                                let text = `*CHUYỂN XU*\n👉Bạn đã *chuyển* ${Helper.numberWithCommas(red)} XU tới người chơi: *${to.name}*\n👉Nội dung: *${data.desc}*\n👉Số dư: ${Helper.numberWithCommas(user.red - red)}`;
                                                                redT.telegram.sendMessage(teleCheck.form, text, { parse_mode: 'markdown', reply_markup: { remove_keyboard: true } });
                                                            }
                                                        });

                                                        var thanhTien = !!daily ? red : Helper.anPhanTram(red, 1, 2);
                                                        var create = { 'from': userAuth.nickname, 'to': to.name, 'red': red, 'red_c': thanhTien, 'time': new Date(), message: data.desc };
                                                        if (void 0 !== data.message && !validator.isEmpty(data.message.trim())) {
                                                            create = Object.assign(create, { message: data.message });
                                                        }

                                                        Phone.findOne({ 'uid': to.id }, {}, function (err, check2) {
                                                            if (check2) {
                                                                telegram.findOne({ 'phone': check2.phone }, 'form', function (err3, teleCheck2) {
                                                                    if (!!teleCheck2) {
                                                                        let text = `*CHUYỂN XU*\n👉Bạn đã *nhận* ${Helper.numberWithCommas(thanhTien)} XU từ: *${userAuth.nickname}*\n👉Nội dung: *${data.desc}*\n👉Số dư: ${Helper.numberWithCommas(to.red * 1 + thanhTien)}`;
                                                                        redT.telegram.sendMessage(teleCheck2.form, text, { parse_mode: 'markdown', reply_markup: { remove_keyboard: true } });
                                                                    }
                                                                });
                                                            } else {
                                                                console.log(`${to.name} chua kich hoat bao mat sdt`);
                                                            }

                                                        });

                                                        ChuyenRed.create(create);
                                                        UserInfo.findOneAndUpdate({ name: to.name }, { $inc: { red: thanhTien } }, function (err, result) {
                                                            if (!!result) {
                                                                if (result.red < thanhTien) {
                                                                    result.daily = userAuth.nickname;
                                                                    result.save();
                                                                }
                                                            let nhiemVu = thanhTien;
                                                            if (nhiemVu > 10000000)
                                                                nhiemVu = 10000000;
                                                            UserMission.updateOne({ uid: result.id, name: result.name, type: 4, active: false, achived: false }, { $set: { active: true, totalPay: nhiemVu, totalAchive: nhiemVu * global.SKnapthe, current: 0, achived: false, time: new Date((new Date()).getTime() + 1728000000) } }).exec();
                                                                if (void 0 !== redT.users[to.id]) {
                                                                    Promise.all(redT.users[to.id].map(function (obj) {
                                                                        obj.red({ notice: { title: 'CHUYỂN XU', text: 'Bạn nhận được ' + Helper.numberWithCommas(thanhTien) + ' XU.' + '\n' + 'Từ người chơi: ' + userAuth.nickname }, user: { red: to.red * 1 + thanhTien } });
                                                                    }));
                                                                }
                                                                UserInfo.findOneAndUpdate({ id: userAuth.id }, { $inc: { red: -red } }).exec();
                                                              //  OTP.updateOne({ '_id': data_otp._id.toString() }, { $set: { 'active': true } }).exec();
                                                                res.json({
                                                                    status: 200,
                                                                    success: true,
                                                                    data: {
                                                                        message: 'Giao dịch thành công.'
                                                                    }
                                                                });
                                                            }
                                                            else {
                                                                console.log(err);
                                                            }
                                                        });
                                                    }
                                                }
                                            } else {
                                                res.json({
                                                    status: 200,
                                                    success: false,
                                                    data: {
                                                        nickname: 'Người dùng không tồn tại.'
                                                    }
                                                });
                                            }
                                        })
                                }
                            } else {
                                res.json({
                                    status: 200,
                                    success: false,
                                    data: {
                                        otp: 'Mã OTP Không đúng.'
                                    }
                                });
                            }
                        });
                    } else {
                        res.json({
                            status: 200,
                            success: false,
                            data: {
                                message: 'Chức năng chỉ dành cho tài khoản đã kích hoạt.'
                            }
                        });
                    }
                });
            }
        }
    }
}
