const _ = require('lodash');
let request = require('request');
var UserInfo      = require('../../Models/UserInfo');
var MomoBonus = require('../../../config/momo.json');
module.exports = function(client){
    var data = new Object();
    data.min =  MomoBonus.min;
    data.max =  MomoBonus.max;
    data.bonus =  MomoBonus.bonus;
    client.red({ shop:{momo:{info:data}}});
}
