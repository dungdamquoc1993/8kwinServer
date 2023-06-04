let UserInfo = require("../../Models/UserInfo");
let DataGiaiThuong    = require('../../../data/sieuzon');

module.exports = function(client){
 //console.log("---vaooooo"+client.UID);
  UserInfo.findOne({id: client.UID},'redPlay name avatar',function(err,user){
     if(!!user){
       UserInfo.countDocuments({redPlay : {$gt : user.redPlay}},function(err,count){
        let rankname;
        let VipPoint;
        var data = new Object();
        data.phuho = new Object();
        data.phuho.top = [];
        //data.phuho.reward = DataGiaiThuong[100001];
        UserInfo.find().sort({redPlay:-1}).limit(20).then(result =>{
         if(!!result){
          result.forEach((item, i) => {
           if(i < 3){
           var topInfo = new Object();
           topInfo.Name = item.name;
            VipPoint = String(item.redPlay/1000000>>0);
           
            if (VipPoint >= 5000) {
                rankname = 'KIM CƯƠNG';
            }else if (VipPoint >= 3000 && VipPoint <= 5000) {
             rankname = 'BẠCH KIM';
             }else if (VipPoint >= 1000 && VipPoint <= 3000) {
                 rankname = 'VÀNG';
             }else if (VipPoint >= 500 && VipPoint <= 1000) {
                 rankname = 'BẠC';
             }else if (VipPoint >= 300 && VipPoint <= 500) {
                 rankname = 'ĐỒNG';
             }else if (VipPoint <= 299) {
                 rankname = 'SẮT';
             }
           topInfo.RankName = rankname;
           topInfo.RedPlay = String(item.redPlay);
           topInfo.Avatar = item.avatar>>0;
           data.phuho.top.push(topInfo);
          }else {
            var topInfo = new Object();
            topInfo.Name = item.name;
             VipPoint = String(item.redPlay/1000000>>0);
             if (VipPoint >= 5000) {
                rankname = 'KIM CƯƠNG';
            }else if (VipPoint >= 3000 && VipPoint <= 5000) {
             rankname = 'BẠCH KIM';
             }else if (VipPoint >= 1000 && VipPoint <= 3000) {
                 rankname = 'VÀNG';
             }else if (VipPoint >= 500 && VipPoint <= 1000) {
                 rankname = 'BẠC';
             }else if (VipPoint >= 300 && VipPoint <= 500) {
                 rankname = 'ĐỒNG';
             }else if (VipPoint <= 299) {
                 rankname = 'SẮT';
             }
            topInfo.RankName = rankname;
            topInfo.RedPlay = String(item.redPlay);
            topInfo.Avatar = item.avatar>>0;
            data.phuho.top.push(topInfo);
          }
          });
          client.red(data);
         }
        });
       });
     }
  });
}
