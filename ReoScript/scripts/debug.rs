///////////////////////////////////////////////////////////////////////////////
//
// Debug script library
// http://www.unvell.com/ReoScript
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
// PURPOSE.
//
// GNU Lesser General Public License (LGPLv3)
//
// lujing@unvell.com
// Copyright (C) unvell, 2012-2013. All Rights Reserved
//
///////////////////////////////////////////////////////////////////////////////

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