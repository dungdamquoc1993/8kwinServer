let mongoose = require('mongoose');

let Schema = new mongoose.Schema({
    name: { type: String, required: false }, // Tên đại lý
    nickname: { type: String, required: true }, // Tên nhân vật trong game
    phone: { type: String, required: false }, // Số điện thoại
    fb: { type: String, default: '' }, // ID Facabook
    location: { type: String, default: '' }, //khu vuc
    vung: { type: String, default: 'vn' }, //khu vuc
    rights: { type: Number, default: 11 }, //quyen daily  mac dinh la dai ly cap 2
    vip: { type: Number, default: 0 }, // điểm vip của đại lý
    lastVip: { type: Number, default: 0 }, // lần đổi ddeiemr trc
    giftcodeBank: { type: Number, default: 0 }, // quy giftcode
    createdBy: { type: String, default: '' } //duoc tao ra boi user nao
});

module.exports = mongoose.model('dailies', Schema);