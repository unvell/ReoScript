/*****************************************************************************
 *
 * ReoScript - .NET Script Language Engine
 *
 * https://github.com/unvell/ReoScript
 *
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
 * PURPOSE.
 *
 * This software released under MIT license.
 * Copyright (c) 2012-2019 Jingwood, unvell.com, all rights reserved.
 *
 ****************************************************************************/

using System;

namespace unvell.ReoScript
{
	public class NumberObject : ObjectValue
	{
		public double Number { get; set; }
		public NumberObject() : this(0) { }
		public NumberObject(double num)
		{
			this.Number = num;
		}
		public override string ToString()
		{
			return Number.ToString();
		}
	}
}
