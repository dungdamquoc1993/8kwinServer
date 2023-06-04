var UserInfo = require('../../../../Models/UserInfo');
var Phones = require('../../../../Models/Phone');
let telegram = require('../../../../Models/Telegram');

module.exports = function(req, res) {
    var { query } = req || {};
    var { id } = query || {};
    UserInfo.updateOne({id:id},{ $set: { 'email': '','cmt':'','otpFirst':false }}, function(err,result) {
     Phones.deleteOne({uid:id},function(err,result){
      if (!err) {
        telegram.deleteOne({uid:id});
          res.json({
              status: 200,
              success: true
          });
      } else {
          res.json({
              status: 200,
              success: false
          });
      }
     })
    })
};
