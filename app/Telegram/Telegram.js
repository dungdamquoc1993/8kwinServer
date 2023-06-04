
let messages = require('./messages');

module.exports = function(bot) {
	bot.on('message', (msg) => {
		console.log(`msg: ${msg}`)
		messages(bot, msg);
	});
}
 