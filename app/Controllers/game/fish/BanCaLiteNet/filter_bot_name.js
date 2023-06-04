const fs = require('fs');

fs.readFile('./data.json', 'utf8', (err, data) => {
    var names = JSON.parse(data);
    var filtered = [];
    console.log('original ' + names.length);
    for (var i = 0; i < names.length; i++) {
        if (names[i].length > 12) {
            names[i] = names[i].substring(0, 12);
        } else if (names[i].length < 6) {
            names[i] = names[i] + Math.round(Math.random() * 1000000);
        }

        if(names[i].indexOf(' ') === -1) {
            names[i] = names[i] + ' ' + Math.round(Math.random() * 1000);
        }

        filtered.push(names[i]);
    }

    console.log('filtered ' + filtered.length);
    fs.writeFile('./data_filtered.json', JSON.stringify(filtered), (err) => {
        if(err)
            console.log('Error in writing file: %j', err.stack);
    });
});

// fs.readFile('./name_4_12.json', 'utf8', (err, data) => {
//     var names = JSON.parse(data);
//     console.log(names.length);
// });