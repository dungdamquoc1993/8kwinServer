
let CronJob = require('cron').CronJob;
let clear  = require('../app/Controllers/admin/panel/HeThong/clear');
module.exports = function (obj) {
	new CronJob('* 1 * * * *', function () {
		clear();
	}, null, true, 'Asia/Ho_Chi_Minh');
}
