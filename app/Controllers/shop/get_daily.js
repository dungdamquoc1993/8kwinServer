var tabDaiLy = require('../../Models/DaiLy');
module.exports = function(client, data) {
    if (data === true) {
        tabDaiLy.find({}, function(err, daily) {
            client.red({ shop: { daily: daily } });
        });
    }else{
        tabDaiLy.find({vung: data}, function(err, daily) {
            client.red({ shop: { daily: daily } });
        });
    }
    

}