
module.exports = function(client){
	let rongho = client.redT.rongho;
	if (rongho.clients[client.UID]) {
		// Bạn hoặc ai đó đang chơi Xóc Xóc bằng tài khoản này
		client.red({notice:{title:'CẢNH BÁO', text:'Bạn hoặc ai đó đang chơi Long Hổ bằng tài khoản này...', load: false}});
	}else{
		// Vào Phòng chơi
		rongho.clients[client.UID] = client;
		client.red({toGame:'RongHo'});

		Object.values(rongho.clients).forEach(function(users){
			if (client !== users) {
				users.red({rongho:{ingame:{client:Object.keys(rongho.clients).length+Math.floor(Math.random() * Math.floor(50))>>0}}});
			}
		});
	}
	rongho = null;
	client = null;
};
