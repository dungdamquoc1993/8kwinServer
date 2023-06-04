
var request = require('request');
var cheerio = require('cheerio');
let xsmb = require('../../../../../Models/XoSo/mb/xsmb');

module.exports = function(client, data) {
	if (!!data.date && !!data.giai1 &&
		Array.isArray(data.giai2) && data.giai2.length === 2 &&
		Array.isArray(data.giai3) && data.giai3.length === 6 &&
		Array.isArray(data.giai4) && data.giai4.length === 4 &&
		Array.isArray(data.giai5) && data.giai5.length === 6 &&
		Array.isArray(data.giai6) && data.giai6.length === 3 &&
		Array.isArray(data.giai7) && data.giai7.length === 4
		)
	{
		xsmb.findOne({date:data.date}, {}, function(err, result) {
			if (!!result) {
				result.gdb = data.giaidb;
				result.g1  = data.giai1;
				result.g2  = data.giai2;
				result.g3  = data.giai3;
				result.g4  = data.giai4;
				result.g5  = data.giai5;
				result.g6  = data.giai6;
				result.g7  = data.giai7;
				result.save();
			}else{
				xsmb.create({date:data.date, gdb:data.giaidb, g1:data.giai1, g2:data.giai2, g3:data.giai3, g4:data.giai4, g5:data.giai5, g6:data.giai6, g7:data.giai7});
			}
			client.red({xs:{mb:{kq:{notice:'Lưu thành công...'}}}});
		});
	}else{
		//data.date.setDate(21);
		xsmb.findOne({date:data.date}, {}, function(err, result) {
			if (!!result) {
				
			}else{
				console.log("data.date="+data.date);
						dateData = data.date.split("/");
						url ='https://xskt.com.vn/xsmb/ngay-'+ parseInt(dateData[0]) + '-' +  parseInt(dateData[1]) + '-'+dateData[2];
						console.log("url= "+url);
						request(url, function (error, response, html) {
							if (!error) {
								var $ = cheerio.load(html,{ decodeEntities: false });
								
								var data1 = $('.result');
								var listDai = data1.find('tr').eq(1);
								//console.log(listDai);
								// giải đặc biệt
								var giaiDB =listDai.find('td').eq(1).text();
								console.log("giải đặc biệt="+giaiDB);
								data.giaidb=giaiDB;
								// giải nhất
								var listgiainhat = data1.find('tr').eq(2);
								var giainhat =listgiainhat.find('td').eq(1).text();
								console.log("giải nhất="+giainhat);
								// giải nhì
								var listgiainhi = data1.find('tr').eq(3);
								var giainhi2 =listgiainhi.find('td').eq(1).text().split(" ");
								console.log("giải nhi="+giainhi2 );

								// giải 3
								var listgiai3 = data1.find('tr').eq(4);
								var giau3 =listgiai3.find('td').eq(1).text();
								giai3plist=(giau3.substring(0, 17) +" "+ giau3.substring(17)).split(" ");

								console.log("giải 3="+giai3plist);
								// rồi split thằng giải 3 ra.

								// giải 4
								var listgiai4 = data1.find('tr').eq(6);
								var giau4 =listgiai4.find('td').eq(1).text().split(" ");
								//giai4plist=
								console.log("giải 4="+giau4);
								// rồi split giau 4 ra

								// giải 5

								var listgiai5 = data1.find('tr').eq(7);
								var giau5 =listgiai5.find('td').eq(1).text();
								giai5plist=(giau5.substring(0, 14) +" "+ giau5.substring(14)).split(" ");
								console.log("giải 5="+giai5plist);

								// giải 6

								var listgiai6 = data1.find('tr').eq(9);
								var giau6 =listgiai6.find('td').eq(1).text().split(" ");
								//giai4plist=
								console.log("giải 6="+giau6);

								// giải 7 

								var listgia7 = data1.find('tr').eq(10);
								var giau7 =listgia7.find('td').eq(1).text().split(" ");
								//giai4plist=
								console.log("giải 7="+giau7);
								//xsmb.create({date:data.date, gdb:data.giaidb, g1:data.giai1, g2:data.giai2, g3:data.giai3, g4:data.giai4, g5:data.giai5, g6:data.giai6, g7:data.giai7});

								xsmb.create({date:data.date, gdb:giaiDB, g1:giainhat, g2:giainhi2, g3:giai3plist, g4:giau4, g5:giai5plist, g6:giau6, g7:giau7});
							}
						});
			}});
	

	}
 }

