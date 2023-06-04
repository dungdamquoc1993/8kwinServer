let UserInfo = require("./app/Models/UserInfo");
let UserMission = require("./app/Models/UserMission");
function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}
module.exports = function(){
    UserInfo.countDocuments({},async function (err, result) {
        let sizeClone = 1000;
        for (let i = 0; i <  (((result / 1000) >> 0) + 1); i++) {
            let skip = sizeClone * i;
            let dataUser = await UserInfo.find({}, {}, { limit: sizeClone, skip: skip });
            for(var j = 0; j < dataUser.length;j++){
                let user = dataUser[j];
                let count = await UserMission.countDocuments({uid:user.id}).exec();
                if(count < 3){
                    UserMission.create({ uid: user.id, name: user.name, type: 1, active: false, totalPay: 1000000, totalAchive: 1000000, current: 0, achived: false, achived2: false, time: new Date() });
					UserMission.create({ uid: user.id, name: user.name, type: 2, active: false, totalPay: 1000000, totalAchive: 1000000, current: 0, achived: false, achived2: false, time: new Date() });
					UserMission.create({ uid: user.id, name: user.name, type: 3, active: false, totalPay: 1000000, totalAchive: 1000000, current: 0, achived: false, achived2: false, time: new Date() });
                }
            }
            await sleep(100);
        }
    })
}