
let mongoose = require('mongoose');

let Schema = new mongoose.Schema({
	type:  {type: String, required: true},	 
	data: {type: String, required: true}, 
    action:  { type: Boolean, default: false },
    date:{ type: Date, default: new Date() },
});
module.exports = mongoose.model('Push', Schema);
