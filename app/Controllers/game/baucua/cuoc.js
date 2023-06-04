
var BauCua_cuoc = require('../../../Models/BauCua/BauCua_cuoc');
var UserInfo    = require('../../../Models/UserInfo');
let Helpers    = require('../../../Helpers/Helpers');
let Push    = require('../../../Models/Push');
module.exports = function(client, data){
	console.log(data.linhVat.red.huou);
	if (!!data && !!data.cuoc) {
		var cuoc    = data.cuoc>>0;
		var red     = true;
		var linhVat = data.linhVat;
		console.log(linhVat);
		if (client.redT.BauCua_time < 5 || client.redT.BauCua_time > 60) {
			client.red({mini:{baucua:{notice: 'Vui lòng cược ở phiên sau.!!'}}});
			return;
		}

		if (linhVat.red.huou === 0 && linhVat.red.bau === 0 && linhVat.red.ga === 0 && linhVat.red.ca === 0 && linhVat.red.cua === 0 && linhVat.red.tom === 0) {
			client.red({mini:{baucua:{notice: 'Cược thất bại...'}}});
		}else{
			UserInfo.findOne({id: client.UID}, 'red xu daily name', function(err, user){
				let tongTien = (linhVat.red.huou + linhVat.red.bau + linhVat.red.ga + linhVat.red.ca + linhVat.red.cua + linhVat.red.tom)*1;
				if (!user || (red && user.red < tongTien) || (!red && user.xu < tongTien)) {
					client.red({mini:{baucua:{notice: 'Bạn không đủ ' + (red ? 'XU':'XU') + ' để cược.!!'}}});
				}else{
					if(red){
						user.red -= tongTien;
					}else{
						user.xu  -= tongTien;
					}
					if (user.daily != '')
						Helpers.pushDailyVIP({ daily: user.daily, reason: "Cộng tiền người chơi " + user.name + " chơi Bầu Cua", type: true, total: tongTien });
					Helpers.MissionAddCurrent(client.UID, (tongTien*0.02>>0));
					user.save();
					var dataXu = [
						'meXuHuou',
						'meXuBau',
						'meXuGa',
						'meXuCa',
						'meXuCua',
						'meXuTom',
					]
					var dataRed = [
						'meRedHuou',
						'meRedBau',
						'meRedGa',
						'meRedCa',
						'meRedCua',
						'meRedTom',
					]
					var tab = red ? dataRed : dataXu;
					var data = {};
					Push.create({
						type:"GameBauCuaBet",
						data:JSON.stringify({name:user.name,money:tongTien,date:new Date()})
					});
					BauCua_cuoc.findOne({uid: client.UID, phien: client.redT.BauCua_phien, red:red}, function(err, checkOne) {
						var io = client.redT;
						if (red) {
							if (linhVat.red.huou != 0) {
								io.baucua.info.redHuou += linhVat.red.huou;
								io.baucua.infoAdmin.redHuou += linhVat.red.huou;
							}else if (linhVat.red.bau != 0) {
								io.baucua.info.redBau += linhVat.red.bau;
								io.baucua.infoAdmin.redBau += linhVat.red.bau;
							}else if (linhVat.red.ga != 0) {
								io.baucua.info.redGa += linhVat.red.ga;
								io.baucua.infoAdmin.redGa += linhVat.red.ga;
							}else if (linhVat.red.ca != 0) {
								io.baucua.info.redCa += linhVat.red.ca;
								io.baucua.infoAdmin.redCa += linhVat.red.ca;
							}else if (linhVat.red.cua != 0) {
								io.baucua.info.redCua += linhVat.red.cua;
								io.baucua.infoAdmin.redCua += linhVat.red.cua;
							}else if (linhVat.red.tom != 0) {
								io.baucua.info.redTom += linhVat.red.tom;
								io.baucua.infoAdmin.redTom += linhVat.red.tom;
							}
						}else{
							if (linhVat == 0) {
								io.baucua.info.xuHuou += cuoc;
								io.baucua.infoAdmin.xuHuou += cuoc;
							}else if (linhVat == 1) {
								io.baucua.info.xuBau += cuoc;
								io.baucua.infoAdmin.xuBau += cuoc;
							}else if (linhVat == 2) {
								io.baucua.info.xuGa += cuoc;
								io.baucua.infoAdmin.xuGa += cuoc;
							}else if (linhVat == 3) {
								io.baucua.info.xuCa += cuoc;
								io.baucua.infoAdmin.xuCa += cuoc;
							}else if (linhVat == 4) {
								io.baucua.info.xuCua += cuoc;
								io.baucua.infoAdmin.xuCua += cuoc;
							}else if (linhVat == 5) {
								io.baucua.info.xuTom += cuoc;
								io.baucua.infoAdmin.xuTom += cuoc;
							}
						}
						if (checkOne){
							var update = {};
							//update[linhVat] = cuoc;
							update[0] = linhVat.red.huou;
							update[1] = linhVat.red.bau;
							update[2] = linhVat.red.ga;
							update[3] = linhVat.red.ca;
							update[4] = linhVat.red.cua;
							update[5] = linhVat.red.tom;	
							BauCua_cuoc.findOneAndUpdate({uid: client.UID, phien: client.redT.BauCua_phien, red:red}, {$inc:update}, function (err, cat){
								Promise.all(tab.map(function(o, i){
									//console.log('o: '+ o);
									//console.log('i: '+ i);
									data[o] = cat[i];
									//console.log(data[o]);
									//console.log(cat[i]);
									return (data[o] = cat[i]);
								}))
								.then(result => {
									
									data[tab[0]] += linhVat.red.huou;
									data[tab[1]] += linhVat.red.bau;
									data[tab[2]] += linhVat.red.ga;
									data[tab[3]] += linhVat.red.ca;
									data[tab[4]] += linhVat.red.cua;
									data[tab[5]] += linhVat.red.tom;
									console.log(data);
									var dataT = {mini:{baucua:{data: data}}, user:{red: user.red, xu:user.xu}};
									Promise.all(client.redT.users[client.UID].map(function(obj){
										obj.red(dataT);
									}));
								})
							});

							Promise.all(io.baucua.ingame.map(function(uOld){
								if (uOld.uid == client.UID && uOld.red == red) {
									//uOld[linhVat] += cuoc;
									uOld[0] += linhVat.red.huou;
									uOld[1] += linhVat.red.bau;
									uOld[2] += linhVat.red.ga;
									uOld[3] += linhVat.red.ca;
									uOld[4] += linhVat.red.cua;
									uOld[5] += linhVat.red.tom;	
								}
							}));
						}else{
							var create = {uid: client.UID, name: client.profile.name, phien: client.redT.BauCua_phien, red:red, time: new Date()};
							create[0] = linhVat.red.huou;
							create[1] = linhVat.red.bau;
							create[2] = linhVat.red.ga;
							create[3] = linhVat.red.ca;
							create[4] = linhVat.red.cua;
							create[5] = linhVat.red.tom;

							BauCua_cuoc.create(create);
							data[tab[0]] = linhVat.red.huou;
							data[tab[1]] = linhVat.red.bau;
							data[tab[2]] = linhVat.red.ga;
							data[tab[3]] = linhVat.red.ca;
							data[tab[4]] = linhVat.red.cua;
							data[tab[5]] = linhVat.red.tom;
							var dataT = {mini:{baucua:{data: data}}, user:{red: user.red, xu:user.xu}};
							Promise.all(client.redT.users[client.UID].map(function(obj){
								obj.red(dataT);
							}));

							var addList = {uid:client.UID, name:client.profile.name, red:red, '0':0, '1':0, '2':0, '3':0, '4':0, '5':0};
							//addList[linhVat] = cuoc;
							addList[0] = linhVat.red.huou;
							addList[1] = linhVat.red.bau;
							addList[2] = linhVat.red.ga;
							addList[3] = linhVat.red.ca;
							addList[4] = linhVat.red.cua;
							addList[5] = linhVat.red.tom;
							io.baucua.ingame.unshift(addList);
						}
					})
				}
			});
		}
	}
};
