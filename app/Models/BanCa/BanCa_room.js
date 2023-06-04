
let AutoIncrement = require('mongoose-auto-increment-reworked').MongooseAutoIncrementID;
let mongoose      = require('mongoose');

let Schema = new mongoose.Schema({
	room:   {type: Number, required: true, index: true}, // phòng (100, 1000, ...)
	player: {type: Number, default: 0},                  // Số người chơi
});

Schema.plugin(AutoIncrement.plugin, {modelName:'BanCa_room', field:'id'});

module.exports = mongoose.model('BanCa_room', Schema);
