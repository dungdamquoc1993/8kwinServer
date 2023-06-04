
let AutoIncrement = require('mongoose-auto-increment-reworked').MongooseAutoIncrementID;
let mongoose      = require('mongoose');

let Schema = new mongoose.Schema({
	rong: {type: Number, required: true},
	ho: {type: Number, required: true},
	chatrong: {type: String, required: true},
	chatho: {type: String, required: true},
	time: {type: Date, default: new Date()},
});

Schema.plugin(AutoIncrement.plugin, {modelName:'RongHo_phien', field:'id'});

module.exports = mongoose.model('RongHo_phien', Schema);
