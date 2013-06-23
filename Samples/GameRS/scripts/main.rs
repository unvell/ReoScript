// GameRS Demo

// battleground is a predefined object in C#
var bg = battleground;

function getRandomInt(max) {
	return Math.floor(Math.random() * max);
}

function Sprite(s) {
	this.s = s;
	this.s.color = rgb(getRandomInt(255), getRandomInt(255), getRandomInt(255));

	this.s.x = getRandomInt(bg.width - 30);
	this.s.y = getRandomInt(bg.height - 30);

	this.horizontalSpeed = Math.random() * 4 + 1;
	this.verticalSpeed = Math.random() * 4 + 1;

	if (Math.random() < 0.5) this.horizontalSpeed *= -1;
	if (Math.random() < 0.5) this.verticalSpeed *= -1;

	this.moveStep = function() {
		if (this.s.x + this.s.width > bg.width || this.s.x < 0) {
			this.horizontalSpeed *= -1;
		}

		if (this.s.y + this.s.height > bg.height || this.s.y < 0) {
			this.verticalSpeed *= -1;
		}

		this.s.x += this.horizontalSpeed;
		this.s.y += this.verticalSpeed;
	};
}

var items = [];

for (var i = 0; i < 10; i++) {
	var s = bg.newSprite();
	var sprite = new Sprite(s);
	items.push(sprite);
}

// entry-point to update frames
function run() {
	for (var i = 0; i < items.length; i++) {
		items[i].moveStep();
	}
}

