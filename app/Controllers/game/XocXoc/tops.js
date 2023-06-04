
var XocXoc_user = require('../../../Models/XocXoc/XocXoc_user');
var UserInfo     = require('../../../Models/UserInfo');
module.exports = function(client, data){
	var red  = !!data;   // Loại tiền (Red: true, Xu: false)
	var project = {uid: '$uid'};
	if (red) {
		project.profit =  {$subtract: ['$red', '$red_lost']};
	}else{
		project.profit =  {$subtract: ['$xu', '$xu_lost']};
	}

	XocXoc_user.aggregate([
		{$project: project},
		{$match:{'profit':{$gt:0}}},
		{$sort: {'profit': -1}},
		{$limit: 50}
	]).exec(function(err, result){
		Promise.all(result.map(function(obj){
			return new Promise(function(resolve, reject) {
				UserInfo.findOne({'id': obj.uid}, 'name', function(error, result2){
					resolve({name: result2.name, bet: obj.profit});
				})
			})
		}))
		.then(function(data){
			//console.log(data);
			client.red({xocxoc:{tops:data}});
		})
	});
};