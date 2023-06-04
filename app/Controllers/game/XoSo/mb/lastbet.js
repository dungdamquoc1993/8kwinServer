var xsmb_cuoc = require('../../../../Models/XoSo/mb/xsmb_cuoc');

module.exports = function(client, date){
		xsmb_cuoc.find({date:date, diem:{$gt:1}}, 'name type cuoc time so', {limit: 100}, function(err, result) {
			Promise.all(result.map(function(obj){
				obj = obj._doc;
				delete obj.__v;
				delete obj._id;
				return obj;
			}))
			.then(function(arrayOfResults) {
				client.red({XoSo:{lastbet:{mb:arrayOfResults}}});
			})
		});
}