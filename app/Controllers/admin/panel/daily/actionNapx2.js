
module.exports = function(req, res) {
    var { body, userAuth } = req || {};
    var { Data: data } = body || {};
    if(data.type == 1){
        res.json({
            status: 200,
            success: true,
            data: {
                SKnapthe: global.SKnapthe
            }
        });
    }else{
        global.SKnapthe = data.SKnapthe*1;
        res.json({
            status: 200,
            success: true,
            data: {
                message: "Bạn đã thay đổi tỉ lệ thành x "+(global.SKnapthe+1),
            }
        });
    }
}