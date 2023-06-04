
let mongoose      = require('mongoose');

let Schema = new mongoose.Schema({
	uid:       {type: String,  required: true},    // ID Người cược
	name:      {type: String,  required: true},    // Name Người cược
	phien:     {type: Number,  required: true, index: true},    // phiên cược
	bet:       {type: Number,  required: true},    // số tiền cược
	so:        {type: Array},                                  // Số chọn
	selectOne:    {type: Boolean},    // bên cược  (true = đặt, false = bỏ)
	selectTwo:    {type: Boolean},    // bên cược  (true = đặt, false = bỏ)
	selectThree:    {type: Boolean},    // bên cược  (true = đặt, false = bỏ)
	thanhtoan: {type: Boolean, default: false},    // tình trạng thanh toán
	win:       {type: Boolean, default: false},	   // Thắng hoặc thua
	betwin:    {type: Number,  default: 0},	       // Tiền thắng được
	time:      {type: Date},                       // thời gian cược
});

Schema.index({uid:1, thanhtoan:1}, {background: true});

module.exports = mongoose.model('bongda_cuoc', Schema);
