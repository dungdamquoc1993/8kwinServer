
module.exports = function (req, res) {
    const { body, userAuth } = req || {};
    res.json({
        status: 200,
        success: true,
        data: {
            VIPCount: global.VIPCount,
            VIPToRIK: global.VIPToRIK
        }
    });
}