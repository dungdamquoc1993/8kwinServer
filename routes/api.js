
let tab_NapThe = require('../app/Models/NapThe');
let MenhGia    = require('../app/Models/MenhGia');
let UserInfo   = require('../app/Models/UserInfo');
let Bank_history = require('../app/Models/Bank/Bank_history');
let config     = require('../config/thecao');
let Helper     = require('../app/Helpers/Helpers');

//let fs = require('fs');

module.exports = function(app, red) {
	// Sign API
	app.get('/api/callback/bank', function(req, res) {
		res.send('ok em yeu');
	});

	app.post('/api/callback/bank', function(req, res) {
		//console.log(body);
		try{
			let data = req.body;
			let info = data.errorCode;
			if(!!data && !!data.errorCode){
				console.log(data);
				if(data.errorCode == "200"){
					Bank_history.findOneAndUpdate({'namego':data.code,'status':0}, {$set:{status:1}}, function(err, napthe) {
						if (!!napthe) {
							if (void 0 !== red.users[napthe.uid]) {
								UserInfo.findOneAndUpdate({'id':napthe.uid}, {$inc:{red:napthe.money}}, function(err2, user) {
									if (void 0 !== red.users[napthe.uid]) {
										Promise.all(red.users[napthe.uid].map(function(obj){
											obj.red({notice:{title:'THÀNH CÔNG', text:'Nạp tiền thành công', load:false}, user:{red:user.red*1+Number(napthe.money)}});
										}));
									}
								});
							}
						}
					});
				}else{
					Bank_history.findOneAndUpdate({'namego':data.code,'status':0}, {$set:{status:2}}, function(err, napthe) {
						if (!!napthe) {
							if (void 0 !== red.users[napthe.uid]) {
								Promise.all(red.users[napthe.uid].map(function(obj){
									obj.red({notice:{title:'THẤT BẠI', text:data.message, load:false}});
								}));
							}
						}
					});
				}
			}
		} catch(errX){
			//
		}
		res.send('info');
	});

	app.get('/api/callback/prepaid_card', function(req, res) {
		res.send('ok em yeu');
	});

	app.post('/api/callback/prepaid_card', function(req, res) {
		try {
			let data = req.body;
			if (!!data && !!data.code && !!data.transaction_user) {
				if (data.code == 'YES') {
					tab_NapThe.findOneAndUpdate({'requestId':data.transaction_user,'status':0}, {$set:{status:1}}, function(err, napthe) {
						console.log(napthe);
						if (!!napthe && napthe.nhan == 0) {
							MenhGia.findOne({name:napthe.menhGia, nap:true}, {}, function(errMG, dataMG){
								if (!!dataMG) {
									let nhan = dataMG.values;
									UserInfo.findOneAndUpdate({'id':napthe.uid}, {$inc:{red:nhan}}, function(err2, user) {
										if (void 0 !== red.users[napthe.uid]) {
											Promise.all(red.users[napthe.uid].map(function(obj){
												obj.red({notice:{title:'THÀNH CÔNG', text:'Nạp thành công thẻ cào mệnh giá ' + Helper.numberWithCommas(dataMG.values), load:false}, user:{red:user.red*1+Number(nhan)}});
											}));
										}
									});
									tab_NapThe.updateOne({'requestId':data.transaction_user}, {$set:{nhan:nhan}}).exec();
								}
							});
						}
					});
				}else{
					tab_NapThe.findOneAndUpdate({'requestId':data.transaction_user,'status':0}, {$set:{status:2}}, function(err, napthe) {
						if (!!napthe) {
							if (void 0 !== red.users[napthe.uid]) {
								Promise.all(red.users[napthe.uid].map(function(obj){
									obj.red({notice:{title:'THẤT BẠI', text:data.message, load:false}});
								}));
							}
						}
					});
				}
			}
		} catch(errX){
			//
		}
		res.send('ok em yeu');
	});
};
