
let UserInfo  = require('../../../../Models/UserInfo');
let tabBongDa	= require('../../../../Models/BongDa/BongDa');
let BongDa_cuoc	= require('../../../../Models/BongDa/BongDa_cuoc');
var Helper  = require('../../../../Helpers/Helpers');

module.exports = function(client, data) {

	if (!!data && !!data.win && !!data.phien) {
		let select = data.win*1;
		var phien = data.phien;
		if (Helper.isEmpty(select) || Helper.isEmpty(phien)) {
			client.red({notice:{title:'KING BET',text:'Không bỏ trống các thông tin...'}});
		}else{
			tabBongDa.findOne({'phien':phien}, function(err,info){
				if (!!info) {
					if (info.status) {
						client.red({notice:{title:'KING BET',text:'Phiên đã trả thưởng...'}});
					}else{
						BongDa_cuoc.find({phien:phien}, {}, function(errC, cuoc){
							if (cuoc.length > 0) {
								client.red({notice:{title:'KING BET',text:'Success'}});
								let tongCuoc = 0;
								let tongTra  = 0;
								info.status = true;
								switch (select) {
									case 1:
										info.ketqua = info.team1;
										break;
									case 2:
										info.ketqua = info.team2;
										break;
									case 3:
										info.ketqua = 'Hòa';
										break;	
								}
								cuoc.forEach(function(objC){
									tongCuoc += objC.bet*1;
									let win = 0;
									let cuoc = objC.bet;
									let trung = 0;
									objC.thanhtoan = true;
									switch (select) {
										case 1:
											// 'Chon doi 1'
											if (objC.selectOne == true) {
												win += info.team1win*cuoc;
											}
											
											break;
										case 2:
											// 'Chon doi 2'
											if (objC.selectTwo == true) {
												win += info.team2win*cuoc;
											}
											break;
										case 3:
											// 'Chon doi Hoa'
											if (objC.selectThree == true) {
												win += info.hoa*cuoc;
											}
											break;
									}
									if (win > 0) {
										tongTra += win;
										objC.betwin = win;
										console.log(objC.name + ' win ' + win);
										UserInfo.updateOne({name:objC.name}, {$inc:{red:win}}).exec();
									}
									objC.save();
								});

								info.cuoc = tongCuoc;
								info.tra = tongTra;
								info.save();
							}else{
								client.red({notice:{title:'KING BET',text:'Không có người cược...'}});
							}

						});
					}
				}else{
					client.red({notice:{title:'KING BET',text:'Trả thưởng thất bại...'}});
				}
			});
		}
	}
	console.log(data);
}
