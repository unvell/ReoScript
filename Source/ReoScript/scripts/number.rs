///////////////////////////////////////////////////////////////////////////////
//
// ReoScript Number Extension
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

if (Number != null && Number.prototype != null) {

  Number.prototype.each = function(iterator) {
    for (var i = 0; i < this; i++) {
      iterator.call(this);
    }
  };
  
}