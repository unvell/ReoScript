

var sw = debug.Stopwatch.startNew();

console.log('start...');

//var c = 500000; //1100ms

for (var i = 0; i < 500000; i++) { }

sw.stop();
console.log('-> ' + i + ' (' + sw.elapsed + ' ms.)');

//console.read();