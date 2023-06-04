
let RongHo_cuoc = require('../../../Models/RongHo/RongHo_cuoc');
let UserInfo    = require('../../../Models/UserInfo');
let Helpers    = require('../../../Helpers/Helpers');
let Push    = require('../../../Models/Push');
module.exports = function(client, data){

	console.log(data);
	if (!!data.cuoc && !!data.box) {
		let cuoc = data.cuoc>>0;
		let red  = !!data.red;
		let box  = data.box;
		if (client.redT.rongho.time < 5 || client.redT.rongho.time > 30) {
			client.red({rongho:{notice: 'Vui lòng cược ở phiên sau.!!'}});
			//return;
		}else if (box.red.rong === 0 && box.red.ho === 0 && box.red.hoa === 0 && box.red.ro === 0 && box.red.co === 0 && box.red.bich === 0 && box.red.tep === 0) {
			client.red({rongho:{notice: 'Cược thất bại...'}});
		}else if (box.red.rong < 0 || box.red.ho < 0 || box.red.hoa < 0 || box.red.ro < 0 || box.red.co < 0 || box.red.bich < 0 || box.red.tep < 0) {
			client.red({rongho:{notice: 'Cược thất bại...'}});
		}else{
			let tongTien = (box.red.rong + box.red.ho + box.red.hoa + box.red.ro + box.red.co + box.red.bich + box.red.tep)*1;
			console.log(tongTien);
			UserInfo.findOne({id:client.UID}, 'red xu daily name', function(err, user){
				if (!user || (red && user.red < tongTien) || (!red && user.xu < tongTien)) {
					client.red({rongho:{notice: 'Bạn không đủ ' + (red ? 'XU':'XU') + ' để cược.!!'}});
				}else{
					if(red){
						user.red -= tongTien;
					}else{
						user.xu  -= tongTien;
					}
					if (user.daily != '')
						Helpers.pushDailyVIP({ daily: user.daily, reason: "Cộng tiền người chơi " + user.name + " chơi RỒNG HỔ", type: true, total: tongTien });
					user.save();
					Helpers.MissionAddCurrent(client.UID, (tongTien*0.02>>0));
					let rongho = client.redT.rongho;

					Push.create({
						type:"GameLongHoBet",
						data:JSON.stringify({name:user.name,money:tongTien,date:new Date()})
					});

					RongHo_cuoc.findOne({uid:client.UID, phien:rongho.phien, red:red}, function(err, checkOne) {
						if (checkOne){
							//checkOne[box] += cuoc;
							checkOne['rong'] += box.red.rong; 
							checkOne['ho'] += box.red.ho; 
							checkOne['hoa'] += box.red.hoa; 
							checkOne['ro'] += box.red.ro; 
							checkOne['co'] += box.red.co; 
							checkOne['bich'] += box.red.bich; 
							checkOne['tep'] += box.red.tep; 
							checkOne.save();
						}else{
							var create = {uid:client.UID,name: client.profile.name, phien:rongho.phien, red:red, time: new Date()};
							//create[box] = cuoc;
							create['rong'] = box.red.rong; 
							create['ho'] = box.red.ho; 
							create['hoa'] = box.red.hoa; 
							create['ro'] = box.red.ro; 
							create['co'] = box.red.co; 
							create['bich'] = box.red.bich; 
							create['tep'] = box.red.tep; 
							RongHo_cuoc.create(create);
						}

						let newData = {
							'rong':   0,
							'ho':     0,
							'hoa':   0,
							'ro':   0,
							'co': 0,
							'bich': 0,
							'tep': 0,
						};
						//newData[box] = cuoc;
						newData['rong'] = box.red.rong; 
						newData['ho'] = box.red.ho; 
						newData['hoa'] = box.red.hoa; 
						newData['ro'] = box.red.ro; 
						newData['co'] = box.red.co; 
						newData['bich'] = box.red.bich; 
						newData['tep'] = box.red.tep; 
						let me_cuoc = {};
						if (red) {
							//rongho.data.red[box] += cuoc;
							//rongho.dataAdmin.red[box] += cuoc;
							rongho.data.red['rong'] += box.red.rong; 
							rongho.data.red['ho'] += box.red.ho; 
							rongho.data.red['hoa'] += box.red.hoa; 
							rongho.data.red['ro'] += box.red.ro; 
							rongho.data.red['co'] += box.red.co; 
							rongho.data.red['bich'] += box.red.bich; 
							rongho.data.red['tep'] += box.red.tep; 
							
							rongho.dataAdmin.red['rong'] += box.red.rong; 
							rongho.dataAdmin.red['ho'] += box.red.ho; 
							rongho.dataAdmin.red['hoa'] += box.red.hoa; 
							rongho.dataAdmin.red['ro'] += box.red.ro; 
							rongho.dataAdmin.red['co'] += box.red.co; 
							rongho.dataAdmin.red['bich'] += box.red.bich; 
							rongho.dataAdmin.red['tep'] += box.red.tep; 

							if (rongho.ingame.red[client.profile.name]) {
								//rongho.ingame.red[client.profile.name][box] += cuoc;
								rongho.ingame.red[client.profile.name]['rong'] += box.red.rong; 
								rongho.ingame.red[client.profile.name]['ho'] += box.red.ho; 
								rongho.ingame.red[client.profile.name]['hoa'] += box.red.hoa; 
								rongho.ingame.red[client.profile.name]['ro'] += box.red.ro; 
								rongho.ingame.red[client.profile.name]['co'] += box.red.co; 
								rongho.ingame.red[client.profile.name]['bich'] += box.red.bich; 
								rongho.ingame.red[client.profile.name]['tep'] += box.red.tep; 
							}else{
								rongho.ingame.red[client.profile.name] = newData;
							}
							me_cuoc.red = rongho.ingame.red[client.profile.name];
						}else{
							rongho.data.xu[box] += cuoc;
							rongho.dataAdmin.xu[box] += cuoc;
							if (rongho.ingame.xu[client.profile.name]) {
								rongho.ingame.xu[client.profile.name][box] += cuoc;
							}else{
								rongho.ingame.xu[client.profile.name] = newData;
							}
							me_cuoc.xu = rongho.ingame.xu[client.profile.name];
						}
						Object.values(rongho.clients).forEach(function(users){
							if (client !== users) {
								users.red({rongho:{chip:{box:box, cuoc:cuoc}}});
							}else{
								users.red({rongho:{mechip:{box:box, cuoc:data.cuoc}, me:me_cuoc}, user:{red:user.red, xu:user.xu}});
							}
						});

						rongho  = null;
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
