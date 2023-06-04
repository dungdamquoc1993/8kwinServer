var DaiLy = require('../../../../Models/DaiLy');
var VIPServices = require('../../../../Models/VIPServices');
let hide = function(param){
    var string = "";
    for(var i = 0; i<param.length;i++){
        string+="*";
    }
    return string;
}
module.exports = function(req, res) {
    var { body, userAuth } = req || {};
    var { Data } = body || {};
    var { fromDate, toDate } = Data || {};
    if(fromDate != null && toDate != null) {
        fromDate = new Date(fromDate);
        toDate = new Date(toDate);
        DaiLy.find({},'com nickname', {sort:{'com':-1}}, function(err, result) {
            Promise.all(result.map(function(obj) {
                return new Promise(function(resolve,reject) {
                    VIPServices.aggregate([
                        {$match:{name:obj.nickname,time:{$gte:fromDate,$lte:toDate},type:true}},
                        {$group:{
                            _id: null,
                            vipTotal:{$sum:"$total"},
                        }}
                    ],function(err,aggResult) {
                        if(void 0 !== aggResult && aggResult.length > 0 && aggResult[0] != null && aggResult[0] !== void 0) {
                            var resData = {};
                            resData.nickname = obj.nickname;
                            resData.vip = hide(""+aggResult[0].vipTotal>>0);
                            resolve(resData);
                        }else{
                            var resData = {};
                            resData.nickname = obj.nickname;
                            resData.vip = 0;
                            resolve(resData);
                        }
                    })
                })
            })).then(resultArr =>{
                res.json({
                    status: 200,
                    success: true,
                    data: resultArr
                });
            })
        })
    }else{
        DaiLy.find({},'vip nickname', {sort:{'vip':-1}}, function(err, result) {
            Promise.all(result.map(function(obj){
                obj = obj._doc;
                obj.vip = obj.vip;
                delete obj._id;
                return obj;
            }))
            .then(resultArr => {
                res.json({
                    status: 200,
                    success: true,
                    data: resultArr
                });
            })
            
        });
    }
};