var Users = require('../../../../Models/Users');
var UserInfo = require('../../../../Models/UserInfo');
var fs = require('fs');
var _ = require('lodash');
module.exports = function (req, res) {
    const { body } = req || {};
    const { Data } = body || {};
    const { nickname, ip, type } = Data || {};
    var finalResult = [];
    if (!!ip) {
        Users.find({ 'local.ip': ip }, function (err, check) {
            if (!!check) {
                var arrFilter = check.map(function (item) {
                    return item._id.toString();
                });
                UserInfo.countDocuments({ id: { $in: arrFilter } }).exec(function (err, totals) {
                    UserInfo.find({ id: { $in: arrFilter } }, {}, { sort: { '_id': -1 }, limit: Data.limit, skip: Data.offset }, function (err, result) {
                        if (result) {
                            var arrFilter2 = result.map(function (item) {
                                return item.id;
                            });
                            Users.find({
                                _id: {
                                    $in: arrFilter2
                                }
                            }, function (err, user) {
                                if (user) {
                                    result.map(function (item, index) {
                                        item = item._doc;
                                        user.map(function (us, index) {
                                            if (us._id == item.id) {
                                                item.ban_login = us.local.ban_login;
                                                item.userID = us._id;
                                                item.ip = us.local.ip;
                                            }
                                            finalResult.push(item);
                                        })
                                    })
                                }
                                res.json({
                                    status: 200,
                                    success: true,
                                    totals: totals,
                                    data: _.uniq(finalResult, function (item) {
                                        return finalResult.UID;
                                    })
                                });
                            });
                        }
                    });
                })
            }
        })
    } else {
        var filter = {};
        if (!!nickname) {
            var regexName = new RegExp('^' + nickname + '$', 'i');
            filter.name = regexName;
        }
        if (!!type) {
            filter.type = type;
        }
        UserInfo.countDocuments(filter).exec(function (err, totals) {
            UserInfo.find(filter, {}, { sort: { '_id': -1 }, limit: Data.limit, skip: Data.offset }, function (err, result) {
                if (result) {
                    var arrFilter = result.map(function (item) {
                        return item.id;
                    });
                    Users.find({
                        _id: {
                            $in: arrFilter
                        }
                    }, function (err, user) {
                        if (user) {
                            result.map(function (item, index) {
                                item = item._doc;
                                user.map(function (us, index) {
                                    if (us._id == item.id) {
                                        item.ban_login = us.local.ban_login;
                                        item.userID = us._id;
                                        item.ip = us.local.ip;
                                    }
                                    finalResult.push(item);
                                })
                            })
                        }
                        res.json({
                            status: 200,
                            success: true,
                            totals: totals,
                            data: _.uniq(finalResult, function (item) {
                                return finalResult.UID;
                            })
                        });
                    });
                }
            });
        });
    }

}