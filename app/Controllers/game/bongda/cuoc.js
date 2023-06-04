var infoBongDa = require('../../../Models/BongDa/BongDa');
var phienBongDa = require('../../../Models/BongDa/BongDa');
var cuocBongDa = require('../../../Models/BongDa/BongDa_cuoc');
var UserInfo = require('../../../Models/UserInfo');
let get_phien = require('./get_phien');
var Helper  = require('../../../Helpers/Helpers');

module.exports = function(client, data) {
    if (!!data && !!data.bet) {
        let bet = data.bet*1;
        let tongTien = 0;
        if (data.SelectOne == false && data.SelectTwo == false && data.SelectThree == false) {
            client.red({notice:{title:"KING BET", text:'Chọn ít nhất một cửa để đặt cược!'}});
            tongTien = 0;
        }else{
            if (bet < 1000) {
                client.red({notice:{title:"KING BET", text:'Tiền cược phải ít nhất 1.000 RIK!'}});
            }else{
                if (data.SelectOne && data.SelectTwo && data.SelectThree == true) {
                    tongTien += bet*3;
                }else{
                    if (data.SelectOne == true) {
                        tongTien += bet*1;
                    }
                    if (data.SelectTwo == true) {
                        tongTien += bet*1;
                    }
                    if (data.SelectThree == true) {
                        tongTien += bet*1;
                    }
                }
                
                UserInfo.findOne({ id: client.UID }, 'red name', function(err, user) {
                    if (!!user && user.red >= tongTien) {
                        
                        
                        phienBongDa.find({ phien: data.phien }, function(err2, phienBongDa) {
                            //check time
                            if (phienBongDa.status) {
                                client.red({notice:{title:"KING BET", text:'Đã hết thời gian cược'}});
                            }else{
                                cuocBongDa.create({'uid':client.UID, 'name': user.name, 'phien': data.phien, 'bet': bet, 'selectOne': data.SelectOne, 'selectTwo': data.SelectTwo, 'selectThree': data.SelectThree, 'thanhtoan': false, 'win': false, 'betwin': '0', 'time': new Date()}, function(err, info){
                                    if (info) {
                                        get_phien(client,info.phien);
                                        //console.log('Tổng tiền cược: ' + tongTien);
                                        client.red({notice:{title:"KING BET", text:'Cược thành công!'}, user:{red:user.red-tongTien}});
                                        user.red -= tongTien;
                                        user.save();
                                        user = null;
                                        tongTien = null;
                                    }
                                    else{
                                        throw(err);
                                        client.red({notice:{title:"KING BET", text:err}});
                                    }
                                });
                            }
                        });
                        
                    }else{
                        client.red({notice:{title:"KING BET", text:'Bạn không đủ tiền cược!'}});
                    }
                    
                });
            }
        }
    }else{
		client.red({notice:{title:"KING BET", text:'Dữ liệu không đúng...'}});
	}
}