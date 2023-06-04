// server.js

require('dotenv').config();
var cors = require('cors');

let Telegram = require('telebot');
let TelegramToken = "2014584094:AAE4qsdteUu5q0NPc9vzmqVBNk4Cq9TikUU";
let TelegramBot = new TeleBot(TelegramToken);


let express = require('express');
let app = express();
app.use(cors({
    origin: '*',
    optionsSuccessStatus: 200
}));
let port = process.env.PORT || 82;
// port = 3000;
let expressWs = require('express-ws')(app);
let bodyParser = require('body-parser');
var morgan = require('morgan');

// Setting & Connect to the Database
let configDB = require('./config/database');
let mongoose = require('mongoose');
// mongoose.set('debug', true);

require('mongoose-long')(mongoose); // INT 64bit

mongoose.set('useFindAndModify', false);
mongoose.set('useCreateIndex', true);
mongoose.connect(configDB.url, configDB.options)
    .catch(function(error) {
        if (error)
            console.log('Connect to MongoDB failed', error);
        else
            console.log('Connect to MongoDB success');
    });

// kết nối tới database

// cấu hình tài khoản admin mặc định và các dữ liệu mặc định
require('./config/admin');
// đọc dữ liệu from
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: false }));
//app.use(morgan('combined'));

app.set('view engine', 'ejs'); // chỉ định view engine là ejs
app.set('views', './views'); // chỉ định thư mục view

// Serve static html, js, css, and image files from the 'public' directory
app.use(express.static('public'));


require('./app/Telegram/Telegram')(TelegramBot); // Telegram Bot

app.listen(port, function() {
    console.log("Server listen on port ", port);
});
