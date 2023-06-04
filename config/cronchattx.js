
let CronJob = require('cron').CronJob;

var UserInfo = require('../app/Models/UserInfo');
var TXChat = require('../app/Models/TaiXiu_chat');

module.exports = function (obj) {
	new CronJob('*/2 * * * * *', function () {
		UserInfo.find({ type: true }, 'id name', function (err, blist) {
			if (blist.length) {
				Promise.all(blist.map(function (buser) {
					buser = buser._doc;
					delete buser._id;

					return buser;
				}))
					.then(result => {
						let botusername = result[(Math.random() * (result.length - 1)) >> 0].name;
						TXChat.find({}, 'value', { sort: { '_id': -1 }, skip: 15, limit: 1000 }, function (err, listchat) {
							if (listchat.length) {
								Promise.all(listchat.map(function (buser) {
									buser = buser._doc;
									delete buser._id;
									return buser;
								})).then(result => {
										setTimeout(function(){
											textchatusername = result[(Math.random() * (result.length - 1)) >> 0].value;
											let content = { taixiu: { chat: { message: { user: botusername, value: textchatusername } } } };
											Promise.all(Object.values(obj.users).map(function (users) {
												Promise.all(users.map(function (member) {
													member.red(content);
												}));
											}));},(Math.random() *1 + 1) * 1000
										);
								});
							}

						});

					});
			}
		});

	}, null, true, 'Asia/Ho_Chi_Minh');
}