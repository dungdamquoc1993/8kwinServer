let AutoIncrement = require('mongoose-auto-increment-reworked').MongooseAutoIncrementID;
let mongoose      = require('mongoose');

let Schema = new mongoose.Schema({
	uid:         {type: String, required: true}, // id
    name:       {type: String, required: true}, // Tên nhân vật
    type: {type: Number, required: true}, // 1 nạp thẻ 2 nạp momo 3 nạp đại lý
    active : {type: Boolean, required: true}, // đang chạy
    totalPay: {type: Number, required: true}, //
    totalAchive : {type: Number, required: true}, //
    current : {type: Number, required: true}, //
    achived : {type: Boolean, required: true}, //nhận thưởng rồi hay chưa
    achived2 : {type: Boolean, required: true}, //nhận thưởng rồi hay chưa
    time: {type: mongoose.Schema.Types.Date, required:true}, // time
});
Schema.index({uid:1,name:1}, {background: true});
Schema.plugin(AutoIncrement.plugin, {modelName: 'usermission', field:'id'});

module.exports = mongoose.model('usermission', Schema);
