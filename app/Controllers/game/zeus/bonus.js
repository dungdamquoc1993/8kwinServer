
let HU                 = require('../../../Models/HU');

const Zeus_red  = require('../../../Models/Zeus/Zeus_red');
const Zeus_xu   = require('../../../Models/Zeus/Zeus_xu');
const Zeus_user = require('../../../Models/Zeus/Zeus_user');

let UserInfo           = require('../../../Models/UserInfo');

function onSelectBox(client, box){
	box = box>>0;
	if (void 0 !== client.Zeus &&
		client.Zeus.bonus !== null &&
		client.Zeus.bonusL > 0)
	{
		let index = box-1;
		if (void 0 !== client.Zeus.bonus[index]) {
			if (!client.Zeus.bonus[index].isOpen) {
				client.Zeus.bonusL -= 1;
				client.Zeus.bonus[index].isOpen = true;

				let bet = client.Zeus.bonus[index].bet;
				client.Zeus.bonusWin += bet;
				client.red({Zeus:{bonus:{bonus: client.Zeus.bonusL, box: index, bet: bet}}});
				if (!client.Zeus.bonusL) {
					let betWin = client.Zeus.bonusWin*client.Zeus.bonusX;

					let uInfo    = {};
					let gInfo    = {};
					let huUpdate = {};

					if (client.Zeus.red) {
						huUpdate.redWin = betWin;
						uInfo.red       = betWin;
						uInfo.redWin    = betWin;
						gInfo.win       = betWin;
						Zeus_red.updateOne({'_id': client.Zeus.id}, {$inc:{win:betWin}}).exec();
					}else{
						huUpdate.xuWin = betWin;
						uInfo.xu       = betWin;
						uInfo.xuWin    = betWin;
						gInfo.winXu    = betWin;

						let thuong = (betWin*0.039589)>>0;
						uInfo.red      = thuong;
						uInfo.thuong   = thuong;
						gInfo.thuong   = thuong;

						Zeus_xu.updateOne({'_id': client.Zeus.id}, {$inc:{win:betWin}}).exec();
					}

					client.Zeus.bonus    = null;
					client.Zeus.bonusWin = 0;
					client.Zeus.bonusX   = 0;

					UserInfo.findOneAndUpdate({id:client.UID}, {$inc:uInfo}, function(err, user){
						setTimeout(function(){
							if (client.Zeus.red) {
								client.red({Zeus:{bonus:{win: betWin}}, user:{red:user.red*1+betWin}});
							}else{
								client.red({Zeus:{bonus:{win: betWin}}, user:{xu:user.xu*1+betWin}});
							}
							client = null;
						}, 700);
					});
					HU.updateOne({game:'Zeus', type:client.Zeus.bet, red:client.Zeus.red}, {$inc:huUpdate}).exec();
					Zeus_user.updateOne({'uid':client.UID}, {$inc:gInfo}).exec();
				}else{
					client = null;
				}
			}
		}
	}
}

module.exports = function(client, data){
	if (void 0 !== data.box) {
		onSelectBox(client, data.box);
	}
};
