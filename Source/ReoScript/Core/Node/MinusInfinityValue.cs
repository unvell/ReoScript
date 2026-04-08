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

namespace unvell.ReoScript
{
	/// <summary>
	/// Runtime -infinity value.
	/// </summary>
	public sealed class MinusInfinityValue : ISyntaxTreeReturn
	{
		public static readonly MinusInfinityValue Value = new MinusInfinityValue();
		private MinusInfinityValue() { }
		public override string ToString()
		{
			return "-Infinity";
		}
	}
}
