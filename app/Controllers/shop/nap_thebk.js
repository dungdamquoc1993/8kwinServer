let tab_NapThe = require('../../Models/NapThe');
let NhaMang = require('../../Models/NhaMang');
let MenhGia = require('../../Models/MenhGia');

let UserInfo = require('../../Models/UserInfo');

let config = require('../../../config/thecao');
let request = require('request');
let validator = require('validator');
let epbank = require('./epbank');
let napthe68 = require('./napthe68');
let mapNhaMangToCode = require('../../Helpers/mapNhaMangToCode');
let crypto = require('crypto');
const _ = require('lodash');
module.exports = function(client, data) {
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
          //  let checkCaptcha = new RegExp('^' + data.captcha + '$', 'i');
           // checkCaptcha = checkCaptcha.test(client.captcha);
            let checkCaptcha = true;
            if (checkCaptcha) {
                let nhaMang = '' + data.nhamang;
                let menhGia = '' + data.menhgia;
                let maThe = '' + data.mathe;
                let seri = '' + data.seri;
                let request_id = ''+Math.floor(Math.random() * Math.floor(99999999999999)) * 2 + 1;
                let check1 = NhaMang.findOne({ name: nhaMang, nap: true }).exec();
                let check2 = MenhGia.find({}).exec();

                Promise.all([check1, check2])
                    .then(values => {
                        if (!!values[0] && !!values[1] && maThe.length > 11 && seri.length > 11) {

                            let nhaMang_data = values[0];
                            let menhGia_data = values[1];

                            tab_NapThe.findOne({ 'uid': client.UID, 'nhaMang': nhaMang, 'menhGia': menhGia, 'maThe': maThe, 'seri': seri }, function(err, cart) {
                                if (cart !== null) {
                                    client.red({ notice: { title: 'THẤT BẠI', text: 'Bạn đã yêu cầu nạp thẻ này trước đây.!!', load: false } });
                                } else {
                                    napthe68.Make({
                                        card_seri: seri,
                                        card_code: maThe,
                                        request_id: request_id,
                                        card_amount: menhGia,
                                        card_type: mapNhaMangToCode(nhaMang)
                                     })
                                     .then(function(response) {
                                         var { status,message,tran_id,amount,real_amount } = response || {};
                                         switch (status) {
                                             case 0:
                                                tab_NapThe.create({ 'uid': client.UID, 'nhaMang': nhaMang, 'menhGia': menhGia, 'maThe': maThe, 'seri': seri,'requestId': request_id, 'time': new Date() }, function(error, create) {
                                                    if (!!create) {
                                                        client.red({notice:{title:'THÔNG BÁO', text:message, load: false}});
                                                    } else {
                                                        client.red({ notice: { title: 'THÔNG BÁO', text: message, load: false } });
                                                    }
                                                });
                                                break;
                                             default:
                                                 client.red({ notice: { title: 'THÔNG BÁO', text: message, load: false } });
                                                 console.log("on case ", response);
                                         }
                                     }, function(err) {
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
