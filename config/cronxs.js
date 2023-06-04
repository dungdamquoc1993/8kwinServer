
let CronJob = require('cron').CronJob;

let update = require('../app/Controllers/admin/game/xs/mb/update');
let trathuong = require('../app/Controllers/admin/game/xs/mb/trathuong');

module.exports = function() {
	new CronJob('0 40 18 * * *', function() {
		var today = new Date();
        var dd = today.getDate();
        var mm = today.getMonth() + 1;
        var yyyy = today.getFullYear();
        if (dd < 10) {
            dd = '0' + dd;
        }
        if (mm < 10) {
            mm = '0' + mm;
        }
		// var today = dd + '/' + mm + '/' + yyyy;
        var today = dd + '/' + mm + '/' + yyyy;
		var data=new Object;
		data.date = today;
		update(null,data);
		new Promise((resolve) => setTimeout(function(){
			console.log("Bắt đầu trả thưởng");
			trathuong(null,data.date);
			console.log("đã trả trúng số xong");
		}, 10000));
		
	}, null, true, 'Asia/Ho_Chi_Minh');
}
