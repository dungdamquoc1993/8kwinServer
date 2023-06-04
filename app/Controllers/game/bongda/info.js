var infoBongDa = require('../../../Models/BongDa/BongDa');
var phienBongDa = require('../../../Models/BongDa/BongDa_phien');

module.exports = function(client, data) {
   
    infoBongDa.create({'date':new Date(), 'team1':'Man U', 'team2': 'Liverpool', 'giaidau': 'Viprik champions', 'ketqua': '', 'team1win': '3', 'team2win': '3', 'hoa': '3', 'status': true, 'cuoc': '0', 'tra': '0'}, function(err, info){
        phienBongDa.create({'phien': info.phien,'nameDoi1': info.team1, 'nameDoi2': info.team2, 'team1win': info.team1win, 'team2win': info.team2win, 'hoa': info.hoa, 'time': new Date()});
    });
    infoBongDa.find({'blacklist':0}, function(err, bongda) {
        //console.log(bongda);
        client.red({mini:{bongda:{info:bongda}}});
    });
};