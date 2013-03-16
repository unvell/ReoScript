///////////////////////////////////////////////////////////////////////////////
//
// ReoScript Array Extension Library
// 
// HP: http://www.unvell.com/ReoScript
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
// PURPOSE.
//
// License: GNU Lesser General Public License (LGPLv3)
//
// Email: lujing@unvell.com
//
// Copyright (C) unvell, 2012-2013. All Rights Reserved
//
///////////////////////////////////////////////////////////////////////////////

if (Array != null && Array.prototype != null) {
  
  // compare two arrays
  Array.prototype.equals = targetArray => {
    if( targetArray == null 
      || this.length != targetArray.length) 
      return false;
    
	  for(var i=0; i < this.length; i++)
		  if( rs[i] != targetArray[i] )
        return false;
			
		return true;
	};
  
  // filter array by given predicate lambda function
	Array.prototype.where = predicate => {
		if(predicate == null) 
			return null;
		
		var result = [];
		
		for(var element in this)
			if( predicate(element) )
				result.push(element);

		return result;
	};

	// sum an array
	Array.prototype.sum = selector => {
    var total = 0;
    
    for(element in this) 
    {
      total += (selector == null ? element : selector(element));
    }

		return total;
	};
	
	// get average of array
	Array.prototype.ave = selector => {
		if( this.length == 0 ) return 0;
		
		return this.sum() / this.length;
	};

  // find min element
	Array.prototype.min = selector => {
    var min = null;
    
    for(element in this) 
    {
      var val = ( selector == null ? element : selector(element) );
      if(min == null || min > val) min = val;
    }

		return min;
	};

  // find max element
	Array.prototype.max = selector => {
    var max = null;
    
    for(element in this) 
    {
      var val = ( selector == null ? element : selector(element) );
      if(max == null || max < val) min = val;
    }

		return max;
	};

  // return the first element
	Array.prototype.first = function() {
		return this.length > 0 ? this[0] : null;
	};
	
	// return the last element
	Array.prototype.last = function() {
		return this.length > 0 ? this[this.length - 1] : null;
	};
}