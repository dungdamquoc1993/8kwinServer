
let mongoose = require('mongoose');
let Schema = new mongoose.Schema({
	uid:     {type:String, required:true, index:true}, // ID người dùng
	room:    {type:Number, default:1},                 // phòng chơi
	fish:    {type:Number, default:0},                 // cá ăn đc
	money:   {type:Number, default:0},                 // Tiền thắng
	time:    Date,                                     // Thời gian vào
});

module.exports = mongoose.model('BanCa_history', Schema);
