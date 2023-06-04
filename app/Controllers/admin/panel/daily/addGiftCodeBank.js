var DaiLy = require('../../../../Models/DaiLy');
module.exports = function(req,res){
    var { body, userAuth } = req || {};
    var { Data: data } = body || {};
    DaiLy.updateOne({nickname:data.nickname},{$inc:{giftcodeBank:data.amount}},function(err,ok){
        if(ok){
            res.json({
                status: 200,
                success: true,
                data: {
                    message: 'Cộng quỹ giftcode cho '+data.nickname+ ' số tiền '+data.amount+' thành công'
                }
            });
        }else{
            res.json({
                status: 200,
                success: false,
                data: {
                    message: 'Cộng quỹ giftcode cho '+data.nickname+ ' số tiền '+data.amount+' thất bại'
                }
            });
        }
    })
}