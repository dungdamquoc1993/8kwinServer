let mongoose = require('mongoose');
let Schema = new mongoose.Schema({
    form:  {type: String,  required: true, unique: true},
    phone: {type: String,  required: true, unique: true},
});
module.exports = mongoose.model('TelegramDaiLyBalance', Schema);