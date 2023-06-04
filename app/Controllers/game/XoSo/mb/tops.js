
var xsmb_cuoc = require('../../../../Models/XoSo/mb/xsmb_cuoc');
module.exports = function(client, date){
		xsmb_cuoc.find({date:date, win:{$gte:1}}, 'name type win', {limit: 50}, function(err, result) {
			Promise.all(result.map(function(obj){
				obj = obj._doc;
				delete obj.__v;
				delete obj._id;
				return obj;
			}))
			.then(function(arrayOfResults) {
				client.red({XoSo:{tops:{mb:arrayOfResults}}});
			})
		});
}