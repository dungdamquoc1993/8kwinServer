var phienBongDa = require('../../../Models/BongDa/BongDa_phien');

module.exports = function(client, data) {
        phienBongDa.find({'phien':data}, function(err, bongda) {
            if (!!bongda) {
                client.red({mini:{bongda:{infophien:bongda}}});
                console.log(bongda);
            }
        });
    };