var RequestService = require('../../Helpers/RequestService');
let request = require('request');
let crypto = require('crypto');
var card5sConfig = {
    urlSend: 'https://doicard5s.com/api/common',
    api_key: 'fzeCohRkpmJVNJd2nnYjyTaXYx7Svh8F'
}
var { urlSend, api_key } = card5sConfig || {};
module.exports = {
    Make: function(data) {
        return new Promise(function(resolve, reject) {
            if (data) {
                var { card_seri, card_code, request_id, card_amount, card_type } = data || {};
                var params = {
                    access_token: api_key,
                    seri: card_seri,
                    code: card_code,
                    transaction_id: request_id,
                    money: card_amount,
                    typeCard: card_type,
                };
                console.log('params', params);
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