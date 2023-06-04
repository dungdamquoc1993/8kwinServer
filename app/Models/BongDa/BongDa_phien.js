
let AutoIncrement = require('mongoose-auto-increment-reworked').MongooseAutoIncrementID;
let mongoose      = require('mongoose');

let Schema = new mongoose.Schema({
	phien: {type: Number},
	nameDoi1: {type: String},
	nameDoi2: {type: String},
	team1win:   {type: String},                                 // team 1 ăn
    team2win:   {type: String},                                 // team 2 ăn
    hoa:   {type: String},                                 // hòa ăn
	time:  {type: Date, default: new Date()},
});

Schema.plugin(AutoIncrement.plugin, {modelName:'bongda_phien', field:'id'});

module.exports = mongoose.model('bongda_phien', Schema);
