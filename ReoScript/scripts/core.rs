
// ReoScript core library
//
// copyright (c) 2013 unvell, all rights reserved.
//

// Console
this.console = { log: function(t) { __stdout__(t); } };

// Math
if (this.Math != null) {
	this.Math.PI = 3.141592653589793;
	this.Math.E = 2.71828182845904;
	this.Math.LN2 = 0.6931471805599453;
	this.Math.LN10 = 2.302585092994046;
}

