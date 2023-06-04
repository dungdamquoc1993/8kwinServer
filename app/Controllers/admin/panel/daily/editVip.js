var DaiLy = require('../../../../Models/DaiLy');
module.exports = function (req, res) {
    const { body, userAuth } = req || {};
    const { Data: data } = body || {};
    global.VIPCount = data.VIPCount;
    global.VIPToRIK =data.VIPToRIK;
    console.log(global.VIPCount,global.VIPtoRIK);
    DaiLy.updateOne({nickname:data.nickname},{$set:{vip:data.vip}}).exec();
    res.json({
        status: 200,
        success: true,
        data: {
            message: "Thay đổi thành công !!!"
        }
    });
}