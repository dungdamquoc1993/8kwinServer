let AutoIncrement = require('mongoose-auto-increment-reworked').MongooseAutoIncrementID;
let mongoose = require('mongoose');

let Schema = new mongoose.Schema({
	//phien:     {type: Number,  required: true, index: true},    // phiên cược
	date: 		{type: String},
	phut: 		{type: String},
	dienbien: 		{type: String},
	blacklist: 		{type: Number},
	team1:     {type: String},                                 // tên đội 1
	team2:      {type: String},                                 // tên đội 2
    giaidau:      {type: String},                                  // Tên giải đấu
	ketqua:      {type: String, default: ''},                                  // kết quả đội thắng thua
    team1win:   {type: String},                                 // team 1 ăn
    team2win:   {type: String},                                 // team 2 ăn
    hoa:   {type: String},                                 // hòa ăn
	status:     {type: Boolean, default: false},                // Trạng thái diễn ra (false: đang, true: end)
	cuoc:    {type: mongoose.Schema.Types.Long, default: 0}, // Tổng cược
	tra:     {type: mongoose.Schema.Types.Long, default: 0}, // Tổng trả
});

//Schema.index({phien:1}, {background: true});
Schema.plugin(AutoIncrement.plugin, {modelName:'bongda', field:'phien'});

module.exports = mongoose.model('bongda', Schema);
