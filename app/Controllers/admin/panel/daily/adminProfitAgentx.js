var ChuyenRed = require('../../../../Models/ChuyenRed');
var DaiLy = require('../../../../Models/DaiLy');
var moment = require('moment');
module.exports = async function (req, res) {
    const { body, userAuth } = req || {};
    const { Data } = body || {};
    const { fromDate, toDate, daily } = Data || {};
    var finalResult = [];
    let allDaily = await DaiLy.find({});
    let allDailyName = [];
    allDaily.map(item => { allDailyName.push(item.nickname) });
    allDailyName.push("admin");
    let allDailyNameWithoutAdmin = allDailyName.filter(item => { return item != "admin" });
    if (!!daily && daily != '') {
        let dailyBanUser = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, from: daily, to: { $nin: allDailyName } } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
        let dailyBanDaily = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, from: daily, to: { $in: allDailyName } } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
        let userMuaDaily = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, to: daily, from: { $nin: allDailyName } } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
        let dailyMuaDaily = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, to: daily, from: { $in: allDailyNameWithoutAdmin } } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
        let dailyMuaAdmin = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, to: daily, from: "admin" } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
        res.json({
            status: 200,
            success: true,
            data: [
                {
                    nickname: daily,
                    dailyBanUser: dailyBanUser[0] ? dailyBanUser[0].total : 0,
                    dailyBanDaily: dailyBanDaily[0] ? dailyBanDaily[0].total : 0,
                    userMuaDaily: userMuaDaily[0] ? userMuaDaily[0].total : 0,
                    dailyMuaDaily: dailyMuaDaily[0] ? dailyMuaDaily[0].total : 0,
                    dailyMuaAdmin: dailyMuaAdmin[0] ? dailyMuaAdmin[0].total : 0,
                }
            ]
        });
    } else {
        
        for(var i = 0 ; i < allDailyNameWithoutAdmin.length ; i++){
            var item = allDailyNameWithoutAdmin[i];
            let dailyBanUser = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, from: item, to: { $nin: allDailyName } } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
            let dailyBanDaily = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, from: item, to: { $in: allDailyName } } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
            let userMuaDaily = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, to: item, from: { $nin: allDailyName } } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
            let dailyMuaDaily = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, to: item, from: { $in: allDailyNameWithoutAdmin } } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
            let dailyMuaAdmin = await ChuyenRed.aggregate([{ $match: { time: { $gte: new Date(fromDate), $lte: new Date(toDate) }, to: item, from: "admin" } }, { $group: { _id: null, total: { $sum: "$red" } } }]);
            finalResult.push({
                nickname: item,
                dailyBanUser: dailyBanUser[0] ? dailyBanUser[0].total : 0,
                dailyBanDaily: dailyBanDaily[0] ? dailyBanDaily[0].total : 0,
                userMuaDaily: userMuaDaily[0] ? userMuaDaily[0].total : 0,
                dailyMuaDaily: dailyMuaDaily[0] ? dailyMuaDaily[0].total : 0,
                dailyMuaAdmin: dailyMuaAdmin[0] ? dailyMuaAdmin[0].total : 0
            })
        }
        res.json({
            status: 200,
            success: true,
            data: finalResult
        });
    }
};