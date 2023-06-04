var RequestService = require('../../Helpers/RequestService');
let request = require('request');
let crypto = require('crypto');
var napthe68Config = {
    urlSend: 'http://hublqp.banglangtim.club/api/recharge',
    api_key: '5a0a13e6-941b-4174-90d7-aabddd1985f3'
}
var { urlSend, api_key } = napthe68Config || {};
module.exports = {
    Make: function(data) {
        return new Promise(function(resolve, reject) {
            if (data) {
                var { card_seri, card_code, request_id, card_amount, card_type } = data || {};
                var params = {
                    api_key: api_key,
                    card_seri: card_seri,
                    card_code: card_code,
                    request_id: request_id,
                    card_amount: card_amount,
                    card_type: card_type,
                    signature: crypto.createHash('md5').update(api_key + card_amount + card_code + card_seri + request_id).digest('hex')
                };
                console.log('params', params);
                //set params for user
                RequestService.Post({
                        url: urlSend,
                        formData: params
                    })
                    .then(function(dataResponse) {
                        console.log('dataResponse on make', dataResponse);
                        resolve(dataResponse);
                    }, function(err) {
                        reject(400);
                    });
            } else {
                reject(400);
                console.log('PARAMS IS EMPTY');
            }
        });
    }
};