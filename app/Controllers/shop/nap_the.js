let tab_NapThe = require('../../Models/NapThe');
let NhaMang = require('../../Models/NhaMang');
let MenhGia = require('../../Models/MenhGia');
let UserInfo = require('../../Models/UserInfo');
let config = require('../../../config/thecao');
let request = require('request');
let validator = require('validator');
let doicard5s = require('./doicard5s');
let mapNhaMangToCode = require('../../Helpers/mapNhaMangToCode');
let crypto = require('crypto');
const _ = require('lodash');
module.exports = function (client, data) {
    //console.log("client>>>>>>>>>>>>>>>>>>", client);
    client = client || {};
    if (!!data && !!data.nhamang && !!data.menhgia && !!data.mathe && !!data.seri && !!data.captcha) {
        if (!validator.isLength(data.captcha, { min: 4, max: 4 })) {
            client.red({ notice: { title: 'LỖI', text: 'Captcha không đúng', load: false } });
        } else if (validator.isEmpty(data.nhamang)) {
            client.red({ notice: { title: 'LỖI', text: 'Vui lòng chọn nhà mạng...', load: false } });
        } else if (validator.isEmpty(data.menhgia)) {
            client.red({ notice: { title: 'LỖI', text: 'Vui lòng chọn mệnh giá thẻ...', load: false } });
        } else if (validator.isEmpty(data.mathe)) {
            client.red({ notice: { title: 'LỖI', text: 'Vui lòng nhập mã thẻ cào...', load: false } });
        } else if (validator.isEmpty(data.seri)) {
            client.red({ notice: { title: 'LỖI', text: 'Vui lòng nhập seri ...', load: false } });
        } else {
            let checkCaptcha = new RegExp('^' + data.captcha + '$', 'i');
            checkCaptcha = checkCaptcha.test(client.captcha);
            if (checkCaptcha) {
                let nhaMang = '' + data.nhamang;
                let menhGia = '' + data.menhgia;
                let maThe = '' + data.mathe;
                let seri = '' + data.seri;
                let request_id = '' + Math.floor(Math.random() * Math.floor(99999999999999)) * 2 + 1;
                let check1 = NhaMang.findOne({ name: nhaMang, nap: true }).exec();
                let check2 = MenhGia.find({}).exec();

                Promise.all([check1, check2])
                    .then(values => {
                        if (!!values[0] && !!values[1] && maThe.length < 30 && seri.length < 30) {
                            let nhaMang_data = values[0];
                            let menhGia_data = values[1];

                            tab_NapThe.findOne({ 'uid': client.UID, 'nhaMang': nhaMang, 'menhGia': menhGia, 'maThe': maThe, 'seri': seri }, function (err, cart) {
                                if (cart !== null) {
                                    client.red({ notice: { title: 'THẤT BẠI', text: 'Bạn đã yêu cầu nạp thẻ này trước đây.!!', load: false } });
                                } else {
                                    doicard5s.Make({
                                        card_seri: seri,
                                        card_code: maThe,
                                        request_id: request_id,
                                        card_amount: menhGia,
                                        card_type: mapNhaMangToCode(nhaMang)
                                    })
                                        .then(function (response) {
                                            var { status, status_code, message, amount, amount_after, message, transaction_id } = response || {};
											console.log(response);
                                            switch (status_code) {
                                                case 9999:
                                                    tab_NapThe.create({ 'uid': client.UID, 'nhaMang': nhaMang, 'menhGia': menhGia, 'maThe': maThe, 'seri': seri, 'requestId': transaction_id, 'time': new Date() }, function (error, create) {
                                                        if (!!create) {
                                                            client.red({ notice: { title: 'THÔNG BÁO', text: message, load: false } });
                                                        } else {
                                                            client.red({ notice: { title: 'THÔNG BÁO', text: message, load: false } });
                                                        }
                                                    });
                                                    break;
                                                case 1009:
                                                    tab_NapThe.create({ 'uid': client.UID, 'status': 1, 'nhaMang': nhaMang, 'menhGia': menhGia, 'maThe': maThe, 'seri': seri, 'requestId': transaction_id, 'time': new Date() }, function (error, create) {
                                                        if (!!create) {
                                                            MenhGia.findOne({ name: menhGia, nap: true }, {}, function (errMG, dataMG) {
                                                                if (!!dataMG) {
                                                                    let nhan = dataMG.values;
                                                                    tab_NapThe.findOneAndUpdate({ 'requestId': transaction_id }, { $set: { nhan: nhan } }, function (err, napthe) {
                                                                        if (!!napthe) {
                                                                            UserInfo.findOneAndUpdate({ id: client.UID }, { $inc: { red: nhan } }, function (err, result) {
                                                                                client.red({ notice: { title: 'THÔNG BÁO', text: 'Nạp thẻ thành công', load: true }, user: { red: result.red * 1 + nhan } });
                                                                            });
                                                                        } else {
                                                                            client.red({ notice: { title: 'THÔNG BÁO', text: "??? 2", load: false } });
                                                                        }
                                                                    });
                                                                }
                                                            });
                                                        } else {
                                                            client.red({ notice: { title: 'THÔNG BÁO', text: "??? 0", load: false } });
                                                        }
                                                    });
                                                    break;
                                                default:
                                                    client.red({ notice: { title: 'THÔNG BÁO', text: message, load: false } });
                                            }
                                        }, function (err) {
                                            console.log("err", err);
                                            client.red({ notice: { title: 'THẤT BẠI', text: 'Hệ thống nạp thẻ tạm thời không hoạt động, Vui lòng quay lại sau.!', load: false } });
                                        });
                                }
                            });
                        } else {
                            client.red({ notice: { title: 'THẤT BẠI', text: 'Thẻ nạp không được hỗ trợ.!!', load: false } });
                        }
                    });
            } else {
                client.red({ notice: { title: 'NẠP THẺ', text: 'Captcha không đúng', load: false } });
            }
        }
    }
    client.c_captcha('chargeCard');
}
