let AutoIncrement = require('mongoose-auto-increment-reworked').MongooseAutoIncrementID;
let mongoose      = require('mongoose');

let Schema = new mongoose.Schema({
    name:       {type: String, required: true}, // Tên đại lý
    reason:       {type: String, required: true}, // Loại cộng -
    type:       {type: Boolean, required: true}, // Loại nhaanj / đổi
    total:       {type: Number, required: true}, // Loại cộng -
    vipFirst:       {type: Number, required: true}, // Loại cộng -
    vipLast:       {type: Number, required: true}, // Loại cộng -
    time: {type: mongoose.Schema.Types.Date, required:true}, // time
});
Schema.index({name:1}, {background: true});
Schema.plugin(AutoIncrement.plugin, {modelName: 'VIPServices', field:'id'});

module.exports = mongoose.model('VIPServices', Schema);