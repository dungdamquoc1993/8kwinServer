var infoBongDa = require('../../../Models/BongDa/BongDa');

module.exports = function(client, data) {
    infoBongDa.find({}, function(err, bongda) {
        client.red({mini:{bongda:{info:bongda}}});
    });
}