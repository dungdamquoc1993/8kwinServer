const _ = require('lodash');
let request = require('request');
var UserInfo      = require('../../Models/UserInfo');
var MomoBonus = require('../../../config/momo.json');
let Bank_history = require('../../Models/Bank/Bank_history');
var validator     = require('validator');
var helper        = require('../../Helpers/Helpers');
let accessToken = `nKsLM9Y20aapKiWBmabaMjAyMS0xMC0xOCAxODoxMDozMQ==`;
let key = `31725a620d80f98adbe87e279d710973`;
let prefix = `King86`;
let kind = `json`;
let type = `Momo`;
//let bankCode = `10010`;
let size = `200x200`;
let Push    = require('../../Models/Push');

module.exports = function(client, data){
    if (!!data && !!data.sotien && !!data.captcha) {
        let money = data.sotien>>0;
         if (validator.isEmpty(data.sotien)) {
            client.red({ notice: { title: '', text: 'Vui lòng nhập số tiền nạp!', load: false } });
        }else if (money < MomoBonus.min) {
			client.red({notice: {title:'LỖI', text: `Nạp tối thiểu ${helper.numberWithCommas(MomoBonus.min)}, tối đa ${helper.numberWithCommas(MomoBonus.max)}`, load: false }});
		}else{
            let checkCaptcha = true;
            if (checkCaptcha) {
                let request_id = ''+Math.floor(Math.random() * Math.floor(99999999999999)) * 2 + 1;
                let form = {
					'kind': kind,
					'name': prefix,
					'key': key,
                    'amount': money,
                    'tranID': request_id,
					'type': type,					
                    //'bankCode': bankCode,
					'size': size,
					'accessToken': accessToken,
                }
                request.post({
                    url: "https://apiay.online/api/info",
                    headers: { 'Content-Type': 'application/json' },
                    json: form,
                }, function (err, httpResponse, body){
					//console.log(body);
                    try{
                        if (body.errorCode == 200) {
                            UserInfo.findOne({id: client.UID}, 'name', function(err, check){
								let data = Buffer.from(body.infomationAccount, 'base64').toString();
                                let nap = JSON.parse(data);
                                nap.syntax = body.comment;
								
                                Bank_history.create({uid:client.UID ,transId: nap.syntax,bank:"momo", number:nap.phone, name:nap.name, namego:check.name, hinhthuc:1, money:money, time:new Date()});
                                
                                client.red({ shop:{momo:{nap:nap}}});
                                //client.red({ notice: { title: '', text: `Vui lòng chuyển tiền tới \n` + money, load: false } });
								client.red({ notice: { title: '', text: `Vui lòng chuyển ${helper.numberWithCommas(money)}, đến ${nap.phone}, \n nội dung ${nap.syntax}`, load: false } });
								Push.create({
									type:"MomoNap",
									data:JSON.stringify({name:check.name,money:money,bank:nap.phone,date:new Date()})
								});
                            });
                        }else{
                            client.red({ notice: { title: '', text: 'Yêu cầu nạp tiền thất bại', load: false } }); 
                        } 
                    }catch(e){
                        //console.log(`??????`);
                        client.red({ notice: { title: '', text: 'Yêu cầu nạp tiền thất bại', load: false } }); 
                    }
                });  
            }
            else{
                client.red({ notice: { title: '', text: 'Mã xác nhận không chính xác!', load: false } });
            }
        }
    }
    client.c_captcha('momoController');

}