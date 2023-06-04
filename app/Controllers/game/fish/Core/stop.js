const http = require('http');
const fs = require('fs');

function post(host, port, path, post_data, cb) {
	// An object of options to indicate where to post to
	var post_options = {
		host: host,
		port: port,
		path: path,
		method: 'POST',
		headers: {
			'Content-Type': 'application/x-www-form-urlencoded',
			'Content-Length': Buffer.byteLength(post_data)
		}
	};

	// Set up the request
	var post_req = http.request(post_options, function (res) {
		const chunks = [];
		res.on('data', data => chunks.push(data));
		res.on('end', () => {
			let body = Buffer.concat(chunks);
			switch (res.headers['content-type']) {
				case 'application/json':
					body = JSON.parse(body);
					break;
			}
			cb(body);
		});
		res.on('error', e => {
			console.error(`Response error: ${e.message}`);
			process.exit(1);
		});
	});
	post_req.on('error', e => {
		console.error(`Request error: ${e.message}`);
		process.exit(1);
	});

	// post the data
	post_req.write(post_data);
	post_req.end();
}

if (!fs.existsSync('./running')) {
	console.log('Server is already stopped.');
	process.exit(0);
}

console.log('Reading config file...');
var configStr = fs.readFileSync('./config.json', 'utf8');
var lines = configStr.split(/\r\n|\r|\n/);
var trimConfig = "";
for (var i = 0; i < lines.length; i++) {
	var line = lines[i].trim();
	if (!line.startsWith('//')) {
		trimConfig += line;
	}
}
var config = JSON.parse(trimConfig);
var servicePort = config['webservice-port'];

post('localhost', servicePort, '/bancaapi/terminate', '', (response) => {
	// response ok
	let interval = setInterval(() => {
		if (fs.existsSync('./running')) {
			console.log('Checking server state...');
		} else {
			console.log('Server is stopped.');
			clearInterval(interval);

			setTimeout(() => {
				const child = require('child_process');
				var out = child.execSync('pm2 ls');
				console.log(out.toString());
				process.exit(0);
			}, 500);
		}
	}, 1000);
});