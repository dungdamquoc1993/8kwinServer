let mongoose = require('mongoose');

let Schema = new mongoose.Schema({
	uid:    {type: String, required: true},
	id:  {type: String, required: true}, 
	createdate: {type: Date,    required: true}, // Thời gian tạo
});

module.exports = mongoose.model('nhiemvu', Schema);