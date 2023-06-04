
let XocXoc_cuoc = require('../../../Models/XocXoc/XocXoc_cuoc');
let UserInfo    = require('../../../Models/UserInfo');
let Helpers    = require('../../../Helpers/Helpers');
let Push    = require('../../../Models/Push');
module.exports = function(client, data){
	if (!!data.cuoc && !!data.box) {
		let cuoc = data.cuoc>>0;
		let red  = !!data.red;
		let box  = data.box;

		if (client.redT.game.xocxoc.time < 5 || client.redT.game.xocxoc.time > 30) {
			client.red({xocxoc:{notice: 'Vui lòng cược ở phiên sau.!!'}});
			//return;
		}else if (box.red.chan === 0 && box.red.le === 0 && box.red.red3 === 0 && box.red.red4 === 0 && box.red.white3 === 0 && box.red.white4 === 0) {
			client.red({mini:{XocXoc:{notice: 'Cược thất bại...'}}});
		}else if (box.red.chan < 0 || box.red.le < 0 || box.red.red3 < 0 || box.red.red4 < 0 ||  box.red.white3 < 0 || box.red.white4 < 0) {
			client.red({mini:{XocXoc:{notice: 'Cược thất bại...'}}});
		}else{
			let tongTien = (box.red.chan + box.red.le + box.red.red3 + box.red.red4 + box.red.white3 + box.red.white4)*1;
			UserInfo.findOne({id:client.UID}, 'red xu name daily', function(err, user){
				if (!user || (red && user.red < tongTien) || (!red && user.xu < tongTien)) {
					client.red({xocxoc:{notice: 'Bạn không đủ ' + (red ? 'XU':'XU') + ' để cược.!!'}});
				}else{
					let phientime = client.redT.game.xocxoc.phien;
					let now 	  =  new Date().getTime();
					if(!client.timeCachexocxoc)
						client.timeCachexocxoc = now - 700;																
					if(now - client.timeCachexocxoc < 700)
						return client.red({xocxoc:{notice: 'Quý khách cược qua nhanh . vui lòng chậm lại !!'}});
						client.timeCachexocxoc = now;						  
					
								 
					if(red){
						user.red -= tongTien;
					}else{
						user.xu  -= tongTien;
					}
					if (user.daily != '')
						Helpers.pushDailyVIP({ daily: user.daily, reason: "Cộng tiền người chơi " + user.name + " chơi Xóc ĐĨa", type: true, total: tongTien });
					user.save();
					Helpers.MissionAddCurrent(client.UID, (tongTien*0.02>>0));
					let xocxoc = client.redT.game.xocxoc;

					//xocxoc.chip[box][cuoc] += 1;

					Push.create({
						type:"GameXocXocBet",
						data:JSON.stringify({name:user.name,money:tongTien,date:new Date()})
					});
					
					XocXoc_cuoc.findOne({uid:client.UID, phien:xocxoc.phien, red:red}, function(err, checkOne) {
						if (checkOne){
							checkOne['chan'] += box.red.chan;
							checkOne['le'] += box.red.le;
							checkOne['red3'] += box.red.red3;
							checkOne['red4'] += box.red.red4;
							checkOne['white3'] += box.red.white3;
							checkOne['white4'] += box.red.white4;
							checkOne.save();
						}else{
							var create = {uid:client.UID,name: client.profile.name, phien:xocxoc.phien, red:red, time: new Date()};
							//create[box] = cuoc;

							create['chan'] = box.red.chan;
							create['le'] = box.red.le;
							create['red3'] = box.red.red3;
							create['red4'] = box.red.red4;
							create['white3'] = box.red.white3;
							create['white4'] = box.red.white4;

							XocXoc_cuoc.create(create);
						}

						let newData = {
							'chan':   0,
							'le':     0,
							'red3':   0,
							'red4':   0,
							'white3': 0,
							'white4': 0,
						};
						//newData[box] = cuoc;
						newData['chan'] = box.red.chan;
						newData['le'] = box.red.le;
						newData['red3'] = box.red.red3;
						newData['red4'] = box.red.red4;
						newData['white3'] = box.red.white3;
						newData['white4'] = box.red.white4;

						let me_cuoc = {};
						if (red) {
							
							//xocxoc.data.red[box] += cuoc;
							xocxoc.data.red['chan'] += box.red.chan;
							xocxoc.data.red['le'] += box.red.le;
							xocxoc.data.red['red3'] += box.red.red3;
							xocxoc.data.red['red4'] += box.red.red4;
							xocxoc.data.red['white3'] += box.red.white3;
							xocxoc.data.red['white4'] += box.red.white4;


							//xocxoc.dataAdmin.red[box] += cuoc;
							xocxoc.dataAdmin.red['chan'] += box.red.chan;
							xocxoc.dataAdmin.red['le'] += box.red.le;
							xocxoc.dataAdmin.red['red3'] += box.red.red3;
							xocxoc.dataAdmin.red['red4'] += box.red.red4;
							xocxoc.dataAdmin.red['white3'] += box.red.white3;
							xocxoc.dataAdmin.red['white4'] += box.red.white4;

							if (xocxoc.ingame.red[client.profile.name]) {
								//xocxoc.ingame.red[client.profile.name][box] += cuoc;
								xocxoc.ingame.red[client.profile.name]['chan'] += box.red.chan;
								xocxoc.ingame.red[client.profile.name]['le'] += box.red.le;
								xocxoc.ingame.red[client.profile.name]['red3'] += box.red.red3;
								xocxoc.ingame.red[client.profile.name]['red4'] += box.red.red4;
								xocxoc.ingame.red[client.profile.name]['white3'] += box.red.white3;
								xocxoc.ingame.red[client.profile.name]['white4'] += box.red.white4;

							}else{
								xocxoc.ingame.red[client.profile.name] = newData;
							}
							me_cuoc.red = xocxoc.ingame.red[client.profile.name];
						}else{
							xocxoc.data.xu[box] += cuoc;
							xocxoc.dataAdmin.xu[box] += cuoc;
							if (xocxoc.ingame.xu[client.profile.name]) {
								xocxoc.ingame.xu[client.profile.name][box] += cuoc;
							}else{
								xocxoc.ingame.xu[client.profile.name] = newData;
							}
							me_cuoc.xu = xocxoc.ingame.xu[client.profile.name];
						}
						Object.values(xocxoc.clients).forEach(function(users){
							if (client !== users) {
								users.red({xocxoc:{chip:{box:box, cuoc:cuoc}}});
							}else{
								users.red({xocxoc:{mechip:{box:box, cuoc:data.cuoc}, me:me_cuoc}, user:{red:user.red, xu:user.xu}});
							}
						});

						xocxoc  = null;
						me_cuoc = null;
						newData = null;
						client  = null;
						data    = null;

						cuoc = null;
						red  = null;
						box  = null;
					})
				}
			});
		}	
	}
};
