
let mongoose = require('mongoose');

let Schema = new mongoose.Schema({
	form:  {type: String}, // ID Telegram
	phone: {type: String}, // Số điện thoại
	uid:       {type: String},    // ID Người cược
	gift:  {type: Boolean, default: false},              // Gift code khởi nghiệp
});

module.exports = mongoose.model('Telegram', Schema);
