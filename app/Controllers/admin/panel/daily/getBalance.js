var UserInfo = require('../../../../Models/UserInfo');
var DaiLy = require('../../../../Models/DaiLy');
module.exports = function(req, res) {
    var { userAuth } = req || {};
    //console.log("userAuth", userAuth);
    Promise.all([
        UserInfo.findOne({id: userAuth.uid}).exec(),
        DaiLy.findOne({nickname: userAuth.nickname}).exec()
    ]).then(response => {
        var user = response[0];
        var daily = response[1];
        res.json({
            status: 200,
            success: true,
            data: {
                balance: user ? user.red : 0,
                giftcodeBank: daily ? daily.giftcodeBank : 0,
            }
        });
    })
};