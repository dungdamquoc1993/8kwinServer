
var History = require('../../../Models/BanCa/BanCa_history');

module.exports = function(client, data){
	if(!!data){
		var page = data>>0; // trang
		if (page < 1) {
			client.red({notice:{text:'DỮ LIỆU KHÔNG ĐÚNG...', title:'THẤT BẠI'}});
		}else{
			var kmess = 10;
			History.countDocuments({uid:client.UID}).exec(function(err, total){
				History.find({uid:client.UID}, 'room fish money time', {sort:{'_id':-1}, skip:(page-1)*kmess, limit:kmess}, function(err, result) {
					Promise.all(result.map(function(obj){
						obj = obj._doc;
						delete obj._id;
						return obj;
					}))
					.then(resultArr => {
						client.red({log:{data:resultArr, page:page, kmess:kmess, total:total}});
					})
				});
			});
		}
	}
};
