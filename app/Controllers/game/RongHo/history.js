
let RongHo_cuoc  = require('../../../Models/RongHo/RongHo_cuoc');
let RongHo_phien = require('../../../Models/RongHo/RongHo_phien');

module.exports = function(client, data){
	if (!!data.page) {
		let red = !!data.red;
		let page = data.page>>0;
		if (page > 0) {
			let kmess = 8;
			RongHo_cuoc.countDocuments({uid:client.UID, red:red, thanhtoan:true}).exec(function(err, total){
				RongHo_cuoc.find({uid:client.UID, red:red, thanhtoan:true}, {}, {sort:{'_id':-1}, skip:(page-1)*kmess, limit:kmess}, function(err, result) {
					if (result.length) {
						Promise.all(result.map(function(obj){
							obj = obj._doc;
							var getPhien = RongHo_phien.findOne({id:obj.phien}).exec();

							return new Promise((ra, loi)=>{
								RongHo_phien.findOne({id:obj.phien}).exec(function(err, dataPhien){
									if (dataPhien) {
										obj.kq = [dataPhien.rong, dataPhien.ho, dataPhien.chatrong, dataPhien.chatho];
									}
									delete obj.__v;
									delete obj._id;
									delete obj.thanhtoan;
									delete obj.uid;
									delete obj.red;
									ra(obj);
								});
							})
							.then(values => {
								return values;
							});
						}))
						.then(function(arrayOfResults) {
							client.red({rongho:{history:{data:arrayOfResults, page:page, kmess:kmess, total:total}}});
						})
					}else{
						client.red({rongho:{history:{data:[], page:page, kmess:kmess, total:0}}});
					}
				});
			})
		}
	}
};
