let Telegram      = require('node-telegram-bot-api');
let helpers  = require('./app/Helpers/Helpers');
let TelegramDaiLyBalance = require("./app/Models/TelegramDaiLyBalance");
let TelegramTokenDaiLy = '5503608568:AAHYaJy0V_0L_lvugO7j-NxkcULzrdyc0R8';
let TelegramBotDaiLy   = new Telegram(TelegramTokenDaiLy, {polling: true});
let Push = require('./app/Models/Push');
let configDB = require('./config/database');
let mongoose = require('mongoose');
var dateFormat = require("dateformat");
require('mongoose-long')(mongoose); // INT 64bit
mongoose.set('useFindAndModify', false);
mongoose.set('useCreateIndex',   true);
mongoose.connect(configDB.url, configDB.options).then(function(){
    console.log("Connect");
    let action = false;
    let timeNow = Date.now;
    let cachePhone = {
        time:0,
        lists:[]
    };
    function InitPhone(set){
        TelegramDaiLyBalance.find({}).then(function( result){
           
            cachePhone.lists = result;
            cachePhone.time = Date.now;
            if(set){
                action = true;
            }
        });
    }
    InitPhone(true)
    setInterval(function(){
        if(action == false) return;
        action = false;
        let _ListsAdmin = [];
        if(cachePhone.lists.length > 0){
            _ListsAdmin = [...cachePhone.lists];
        }
        if(Date.now - cachePhone.time > 60000){
            InitPhone(false);
        }
        Push.findOne({'action':false}, {}, {sort:{'_id':-1}, skip:0, limit:1}, function(err, result) {
            if(result){
               
                let data = JSON.parse(result.data);
                console.log(data);
                let Msg = "";
                let Isdelete = false;    
                if(result.type == "GameTXBet"){
                    if(data.select){
                        Msg+="[Người chơi]  "+data.name+" Đang đặt cược Tài Xỉu cửa tài ["+data.money+"]  "+dateFormat( result.date, "isoDate");
                    }else{
                        Msg+="[Người chơi]  "+data.name+" Đang đặt cược Tài Xỉu cửa xỉu ["+data.money+"] "+dateFormat( result.date, "isoDate");
                    }
                    Isdelete = true;
                }else if(result.type == "ChuyenTien"){
                    Msg+=data.msg+" => "+dateFormat( result.date, "isoDateTime");
                    Isdelete = true;
                }else if(result.type == "GameXocXocBet"){
                    Msg+="[Người chơi] "+data.name+" Đang đặt cược Xóc đĩa ["+data.money+"] "+dateFormat( result.date, "isoDate");
                    Isdelete = true;
                }else if(result.type == "GameLongHoBet"){
                    Msg+="[Người chơi] "+data.name+" Đang đặt cược Long hổ ["+data.money+"] "+dateFormat( result.date, "isoDate");
                    Isdelete = true;
                }else if(result.type == "GameBauCuaBet"){
                    Msg+="[Người chơi] "+data.name+" Đang đặt cược bầu cua ["+data.money+"]  "+dateFormat( result.date, "isoDate");
                    Isdelete = true;
                }else if(result.type == "BankNap"){
                    Msg+="[BANK NAP]  ["+data.money+"] vnd bank:"+data.bank+' Name:'+data.name + " time:" +result.date.toString();
                }else if(result.type == "MomoNap"){
                    Msg+="[Momo NAP]  ["+data.money+"] vnd momo:"+data.bank+' Name:'+data.name + " time:" +result.date.toString();
                }else if(result.type == "BankRut"){
                    Msg+="[BANK RUT]  ["+data.money+"] vnd bank:"+data.bank+' Name:'+data.name + " time:" +result.date.toString();
                }else if(result.type == "DoiThe"){
                    Msg+="[DOI THE]  ["+data.nhaMang+"] menh gia:"+data.menhGia+' Name:'+data.name + " time:" +result.date.toString();
				}else if(result.type == "Admin_Update_Bank"){
                    Msg+="[ADMIN Update Bank Nap]  ["+data.money+"] vnd bank:"+data.bank+' Name:'+data.name;
                    if(data.type == 1){
                        Msg+=" Duyệt";
                    }else if(data.type == 10){
                        Msg+=" Hủy Duyệt";
                    }else if(data.type == 404){
                        Msg+=" Xoa";
                    }
                    else{
                        Msg+=" Hủy";
                    }
                }else{
                    Msg+=result.data;
                }
				console.log(_ListsAdmin);
                if(Msg.length > 0){
                    _ListsAdmin.map(function(val,i){
                        TelegramBotDaiLy.sendMessage(
                            val.form,
                            Msg,
                            {parse_mode:'markdown', reply_markup:{remove_keyboard:true}});
                    });
                }
               
                if(Isdelete){
                    Push.deleteOne({_id:result._id}).then(function(){
                        action = true;
                    });
                }else{
                    Push.updateOne({_id:result._id},{ $set: {action: true} },function(){
                        action = true;
                    });
                }
            }else{
                action = true;
            }
        });
    },100);	

	TelegramBotDaiLy.on('message',(msg)=>{
		
        console.log(msg);

        if(msg.hasOwnProperty('contact')){
            //console.log("111111")
            let id = msg.from.id;
            console.log("id:"+id);
            let contact = msg.contact.phone_number;

            console.log("contact:"+contact);
            let phoneCrack;
            try{
                phoneCrack = helpers.phoneCrack(contact);
                console.log("phoneCrack"+phoneCrack)
            }catch (e) {
                console.log(e.toString());
            }
            if (phoneCrack) {
                TelegramDaiLyBalance.findOne({'phone':phoneCrack.phone}, 'uid region phone', function(err, check1){
                    if (!check1) {
                        try {
                            TelegramDaiLyBalance.create({'form':id, 'phone':phoneCrack.phone}, function(err, cP){
                                phoneCrack = null;
                                if (!!cP) {
                                    console.log('id:'+id);
                                    TelegramBotDaiLy.sendMessage(id,
                                        'Bạn nhận được thông báo khi có người chơi hoạt động.',
                                        {parse_mode: 'markdown',reply_markup: {remove_keyboard: true}
                                        });
                                }else{
                                    TelegramBotDaiLy.sendMessage(id, '_Thao tác thất bại_', {parse_mode: 'markdown',reply_markup: {remove_keyboard: true}});
                                }
                            });
                        } catch (error) {
                            TelegramBotDaiLy.sendMessage(id, '_Thao tác thất bại_', {parse_mode: 'markdown',reply_markup: {remove_keyboard: true}});
                        }
                    }
                });
            }
        }else{
			
            TelegramDaiLyBalance.findOne({'form':msg.from.id}, 'phone', function(err, data){
                if (data) {
                    let opts = {
                        parse_mode: 'markdown',
                        reply_markup: {
                            remove_keyboard: true,
                        }
                    };
                    TelegramBotDaiLy.sendMessage(msg.from.id, '*HƯỚNG DẪN*' + 'Hệ thống thông báo', opts);
                }else{
                    let opts = {
                        parse_mode: 'markdown',
                        reply_markup: {
                            keyboard: [
                                [{text: 'CHIA SẺ SỐ ĐIỆN THOẠI', request_contact: true}],
                            ],
                            resize_keyboard: true,
                        }
                    };
                    TelegramBotDaiLy.sendMessage(msg.from.id, '*Q36.VIN*  Hệ thống thông báo thông minh. Vui lòng ấn CHIA SẺ SỐ ĐIỆN THOẠI để XÁC THỰC', opts);
                }
            });
        }
    });

}); // kết nối tới database