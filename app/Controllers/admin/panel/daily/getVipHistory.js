var VIPServices = require('../../../../Models/VIPServices');
module.exports = function (req, res) {
    const { body, userAuth } = req || {};
    const { Data } = body || {};
    VIPServices.countDocuments({ name: userAuth.nickname }, function (err, totals) {
        VIPServices.find({
            name: userAuth.nickname
        }, {}, { sort: { '_id': -1 }, limit: Data.limit, skip: Data.offset }, function (err, result) {
            Promise.all(result.map(function (obj) {
                obj = obj._doc;
                delete obj.__v;
                delete obj._id;
                return obj;
            })).then(arrayResult => {
                res.json({
                    status: 200,
                    success: true,
                    data: arrayResult,
                    totals:totals
                });
            })

        });
    });
};