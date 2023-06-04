const _ = require('lodash');
let request = require('request');
var UserInfo      = require('../../Models/UserInfo');
var BankingBonus = require('../../../config/banking.json');
module.exports = function(client){
    var data = new Object();
    data.min =  BankingBonus.min;
    data.max =  BankingBonus.max;
    data.bonus =  BankingBonus.bonus;
    client.red({ shop:{banking:{info:data}}});
}
