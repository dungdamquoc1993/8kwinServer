
let mongoose = require('mongoose');

let Schema = new mongoose.Schema({
	uid:   {type: String,  required: true},              // ID Người cược
	name:  {type: String,  required: true},              // Tên Người cược
	phien: {type: Number,  required: true, index: true}, // phiên cược
	red:   {type: Boolean, required: true},              // loại tiền (Red = true,   Xu = false)

	rong:      {type: Number,  default: 0},         // Số tiền đặt rong
	ho:        {type: Number,  default: 0},         // Số tiền đặt ho
	hoa:      {type: Number,  default: 0},         // Số tiền đặt hoa
	ro:      {type: Number,  default: 0},         // Số tiền đặt ro
	co:      {type: Number,  default: 0},         // Số tiền đặt co
	bich:    {type: Number,  default: 0},         // Số tiền đặt bich
	tep:    {type: Number,  default: 0},         // Số tiền đặt tep

	thanhtoan: {type: Boolean, default: false},     // tình trạng thanh toán
	betwin:    {type: Number,  default: 0},	        // Tiền thắng được
	time:      {type: Date},                        // thời gian cược
});

Schema.index({uid:1, red:1, thanhtoan:1}, {background: true});
Schema.index({uid:1, phien:1, red:1}, {background: true});

module.exports = mongoose.model('RongHo_cuoc', Schema);
