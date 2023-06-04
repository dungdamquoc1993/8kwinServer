
var tabTranDau   = require('../../../../Models/BongDa/BongDa');
var phienBongDa = require('../../../../Models/BongDa/BongDa_phien');
var Helper  = require('../../../../Helpers/Helpers');


module.exports = function(client, data) {
    //console.log(data);
	if (!!data && !!data.giaidau && !!data.doi1 && !!data.doi2 && !!data.team1win && !!data.team2win && !!data.hoa && !!data.date) {
		var giaidau         = data.giaidau;
		var doi1            = data.doi1
		var doi2            = data.doi2;
        var team1win        = data.team1win;
        var team2win        = data.team2win;
        var hoa             = data.hoa;
        var date            = data.date;
        var phut            = data.phut;
        
		if (Helper.isEmpty(giaidau) || Helper.isEmpty(doi1) || Helper.isEmpty(doi2) || Helper.isEmpty(team1win) || Helper.isEmpty(team2win) || Helper.isEmpty(hoa) || Helper.isEmpty(date)) {
			client.red({notice:{title:'KING BET',text:'Không bỏ trống các thông tin...'}});
		}else{

            tabTranDau.create({'giaidau':giaidau, 'team1':doi1, 'team2':doi2, 'team1win':team1win, 'team2win':team2win, 'hoa':hoa, 'date':date, 'phut':phut, 'dienbien': 'Đang cập nhật..', 'blacklist':0}, function(errC, dataC) {
                if (!!dataC) {
                    phienBongDa.create({'phien': dataC.phien,'nameDoi1': dataC.team1, 'nameDoi2': dataC.team2, 'team1win': dataC.team1win, 'team2win': dataC.team2win, 'hoa': dataC.hoa, 'time': new Date()});
                    tabTranDau.find({'blacklist':0}, function(err, data){
                        client.red({bongda:{data:data}, notice:{title:'KING BET',text:'Thêm thành công...'}});
                    });
                }
                else{
                    client.red({notice:{title:'KING BET',text:'Có lỗi xảy ra, xin vui lòng thử lại.'}});
                }
            });
		}
	}
}