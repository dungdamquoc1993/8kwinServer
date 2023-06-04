require('dotenv').config();
var cors = require('cors');
const path = require('path');

let express = require('express');
let app = express();
app.use(cors({
    origin: '*',
    optionsSuccessStatus: 200
}));
let port = process.env.PORT || 8090;
// port = 3000;
let expressWs = require('express-ws')(app);
let bodyParser = require('body-parser');
var morgan = require('morgan');
// đọc dữ liệu from
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: false }));
//app.use(morgan('combined'));

app.engine('html', require('ejs').renderFile);
app.set('view engine', 'html');
app.set('views', path.join(__dirname, 'views'));

// Serve static html, js, css, and image files from the 'public' directory
app.use(express.static(path.join(__dirname, 'public')));

app.listen(port, function() {
    console.log("Server listen on port ", port);
});