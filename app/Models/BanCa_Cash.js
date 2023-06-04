
let mongoose = require('mongoose');

let Schema = new mongoose.Schema({
	name: {type: String, required: true, index: true},
	cash: {type: Number,   default:0},         // Loại được ăn lớn nhất trong phiên
	type: {type: String,   default:'CashDefault'},         // Loại được ăn lớn nhất trong phiên
	time: {type: Date,   default: new Date()},
});

module.exports = mongoose.model('BanCa_Cashs', Schema);
