var infoBongDa = require('../../../Models/BongDa/BongDa');
var phienBongDa = require('../../../Models/BongDa/BongDa');
var cuocBongDa = require('../../../Models/BongDa/BongDa_cuoc');
var UserInfo = require('../../../Models/UserInfo');

module.exports = function(client, data) {
    if (!!data) {
        var phien = data >> 0;
        //var taixiu = !!data.taixiu;
        //var red = !!data.red;

        var getPhien = phienBongDa.findOne({ phien: phien }).exec();

        var getCuoc = cuocBongDa.find({ phien: phien}, null).exec();

        var tong_L = 0;
        var tong_R = 0;
        var tong_C = 0;
        var tong_bancuoc_L = 0;
        var tong_bancuoc_R = 0;
        var tong_bancuoc_C = 0;

        Promise.all([getPhien, getCuoc]).then(values => {
            if (!!values[0]) {
                var infoPhienCuoc = values[0];
                var phienCuoc = values[1];

                var dataT = {};
                dataT['phien'] = phien;
                dataT['time'] = infoPhienCuoc.time;
                var dataL = new Promise((resolve, reject) => {
                    UserInfo.findOne({ id: client.UID }, 'name', function(err, user) {
                    Promise.all(phienCuoc.filter(function(obj) {
                            if (obj.selectOne == true) {
                                tong_L += obj.bet
                                if (obj.name == user.name) {
                                    tong_bancuoc_L += obj.bet;
                                }
                            }
                            if(obj.selectTwo == true) {
                                tong_R += obj.bet
                                if (obj.name == user.name) {
                                    tong_bancuoc_R += obj.bet;
                                }
                            }
                            if(obj.selectThree == true) {
                                tong_C += obj.bet
                                if (obj.name == user.name) {
                                    tong_bancuoc_C += obj.bet;
                                }
                            }
                            return obj.selectOne == 1
                        
                        }))
                        .then(function(arrayOfResults) {
                            resolve(arrayOfResults)
                        })
                    }); 
                });
                var dataR = new Promise((resolve, reject) => {
                    Promise.all(phienCuoc.filter(function(obj) {
                            return obj.selectTwo == 1
                        }))
                        .then(function(arrayOfResults) {
                            resolve(arrayOfResults)
                        })
                });
                var dataC = new Promise((resolve, reject) => {
                    Promise.all(phienCuoc.filter(function(obj) {
                            return obj.selectThree == 1
                        }))
                        .then(function(arrayOfResults) {
                            resolve(arrayOfResults)
                        })
                });
                Promise.all([dataL, dataR, dataC]).then(result => {
                    dataT['tong_L'] = tong_L;
                    dataT['tong_R'] = tong_R;
                    dataT['tong_C'] = tong_C;
                    dataT['tong_bancuoc_L'] = tong_bancuoc_L;
                    dataT['tong_bancuoc_R'] = tong_bancuoc_R;
                    dataT['tong_bancuoc_C'] = tong_bancuoc_C;
                    dataT['dataL'] = result[0];
                    dataT['dataR'] = result[1];
                    dataT['dataC'] = result[2];
                    client.red({mini:{bongda:{getphien:dataT}}});
                });
            } else {
                client.red({ notice: { title: 'LỖI', text: 'Phiên không tồn tại...', load: false } });
            }
        });
    }
}