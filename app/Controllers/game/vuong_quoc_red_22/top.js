
var VuongQuocRed_red = require('../../../Models/VuongQuocRed/VuongQuocRed_red');
var VuongQuocRed_xu  = require('../../../Models/VuongQuocRed/VuongQuocRed_xu');

var UserInfo  = require('../../../Models/UserInfo');

module.exports = function(client, data){
	if (!!data && !!data.page) {
		var page = data.page>>0; // trang
		var red  = !!data.red;   // Loại tiền (Red: true, Xu: false)
		if (page < 1) {
			client.red({notice:{text: 'DỮ LIỆU KHÔNG ĐÚNG...', title: 'THẤT BẠI'}});
		}else{
			var kmess = 8;
			if (red) {
				VuongQuocRed_red.countDocuments({type:{$gte:1}}).exec(function(err, total){
					VuongQuocRed_red.find({type:{$gte:1}}, 'name win bet time type', {sort:{'_id':-1}, skip: (page-1)*kmess, limit: kmess}, function(err, result) {
						Promise.all(result.map(function(obj){
							obj = obj._doc;
							delete obj._id;
							return obj;
						}))
						.then(resultArr => {
							client.red({VuongQuocRed:{top:{data:resultArr, page:page, kmess:kmess, total:total}}});
						})
					});
				})
			}else{
				VuongQuocRed_xu.countDocuments({type:{$gte:1}}).exec(function(err, total){
					VuongQuocRed_xu.find({type:{$gte:1}}, 'name win bet time type', {sort:{'_id':-1}, skip: (page-1)*kmess, limit: kmess}, function(err, result) {
						Promise.all(result.map(function(obj){
							obj = obj._doc;
							delete obj._id;
							return obj;
						}))
						.then(resultArr => {
							client.red({VuongQuocRed:{top:{data:resultArr, page:page, kmess:kmess, total:total}}});
						})
					});
				})
			}
		}
	}
};