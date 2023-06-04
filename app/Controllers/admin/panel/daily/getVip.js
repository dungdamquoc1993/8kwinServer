var DaiLy = require('../../../../Models/DaiLy');
var VIPServices = require('../../../../Models/VIPServices');
var moment = require('moment-timezone');
module.exports = function(req, res) {
    var { userAuth } = req || {};
    var fromDate = new Date(moment().tz('Asia/Ho_Chi_minh').format('YYYY-MM-DD 00:00:00 Z'));
    var toDate = new Date(moment().tz('Asia/Ho_Chi_minh').format('YYYY-MM-DD 23:59:59 Z'));
    Promise.all([
        DaiLy.findOne({nickname: userAuth.nickname}).exec(),
        VIPServices.aggregate([
            {$match:{name:userAuth.nickname,type:true,time: {
            $gte:  fromDate,
            $lte:  toDate
            }}},
            {$group:{
                _id:null,
                total:{$sum:"$total"}
            }}
        ])
    ]).then(result => {
        var meData = result[0];
        var meToday = result[1];
        var meTodayVip = 0;
        if(void 0 !== meToday && void 0 !== meToday[0]){
            meTodayVip = meToday[0].total;
        }
        res.json({
            status: 200,
            success: true,
            data: {
                vip : meData ? meData.vip>>0 : 0,
                lastVip : meData ? meData.lastVip>>0 : 0,
                todayVip : (meTodayVip)>>0,
            }
        });
    })

};