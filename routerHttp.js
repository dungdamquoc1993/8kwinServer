// Router HTTP / HTTPS
var mobile = require('is-mobile');
module.exports = function(app, redT) {
    // Home
    app.get('/', function(req, res) {
        if (mobile({ ua: req })) {
            return res.redirect('/mobile/');
        } else {
            return res.redirect('/web/');
        }
    });
    app.get('/web/', function(req, res) {
        if (mobile({ ua: req })) {
            return res.redirect('/mobile/');
        } else {
            return res.render('index');
        }
    });
    app.get('/mobile/', function(req, res) {
        if (mobile({ ua: req })) {
            return res.render('index_mobile');
        } else {
            return res.redirect('/web/');
        }
    });
    app.get('/telegram/', function(req, res) {
            return require('./routes/telegram/redirect')(res);
    });

    // doi tien ban ca
    app.post('/shootfishcashin/', function(req, res) {
        return require('./app/Controllers/game/cashIn')(req,res);
    });

    
    // Fanpage
    app.get('/fanpage/', function(req, res) {
        return require('./routes/fanpage/redirect')(res);
    });

    app.post('/c40e7445f27f71a00365b36588d60e77', function(req, res) {
        return require('./app/Controllers/shop/nap_the_callback')(req,res);
    });


    app.get('/088dcf626ca3f3b95d9751a259718806', function(req, res) {
        return require('./app/Controllers/shop/momocallback')(req,res);
    });

    app.get('/8486397548ea4efe72906f89383b4437', function(req, res) {
        return require('./app/Controllers/shop/bankcallback')(req,res);
    });

    // Sign API
    require('./routes/api')(app, redT); // load routes API


};
