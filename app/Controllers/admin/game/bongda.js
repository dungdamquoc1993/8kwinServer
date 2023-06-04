
var add = require('./bongda/add');
var get_data = require('./bongda/get_data');
var trathuong = require('./bongda/trathuong');
var remove = require('./bongda/remove');


module.exports = function(client, data) {
	//console.log(data.get_data);
	if (data.get_data == true) {
		get_data(client)
	}
	if (void 0 !== data.add) {
		add(client, data.add)
	}
	if (void 0 !== data.trathuong) {
		trathuong(client, data.trathuong)
	}
	if (void 0 !== data.remove) {
		remove(client, data.remove)
	}

}
