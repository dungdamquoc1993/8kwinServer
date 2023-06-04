
let CronJob = require('cron').CronJob;
var TXChat      = require('./../app/Models/TaiXiu_chat');


module.exports = function () {
	var flag=true;
	new CronJob('*/20 * * * * *', function () {
		
		if (flag){
			var lineReader = require('readline').createInterface({
				input: require('fs').createReadStream('./data/textchat.txt')
				  });
				  lineReader.on('line', function (line) {
					  if(line != null || line != "" || line != " "){
						TXChat.create({'uid':'1123456789', 'name':"iloveyou", 'value':line});
					  }
				  });

			  flag=false;	  
			  console.log("------------------DONE CREATE COPY TEXT CHAT VAO BANG GAME-------------------");
		}
		
		// fs.readFile('./data/textchat.txt', 'utf8', (errjs, taixiujs) => {
		// 	//var taixiujs = JSON.parse(taixiujs);
		// 	console.log(taixiujs);
		// });
	}, null, true, 'Asia/Ho_Chi_Minh');
}
