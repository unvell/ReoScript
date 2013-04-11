// Debug script library

if (debug != null) {

  /* Stopwatch */

  debug.Stopwatch = function() {
    this.startTime = null;
    this.endTime = null;
  }

  debug.Stopwatch.prototype.start = function() {
    if (this.startTime == null) {
      this.startTime = new Date();
    }
    this.endTime = null;
  };

  debug.Stopwatch.prototype.stop = function() {
    this.endTime = new Date();
    this.elapsed = this.endTime.subtract(this.startTime);
  };

  debug.Stopwatch.prototype.restart = function() {
    this.startTime = new Date();
    this.endTime = null;
  };

  /* End of Stopwatch */

}