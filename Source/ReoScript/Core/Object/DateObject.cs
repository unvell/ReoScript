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
	public class DateObject : ObjectValue
	{
		public static readonly long StartTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

		public DateTime DateTime;

		public DateObject(DateTime value)
		{
			this.DateTime = value;

			this["getFullYear"] = new NativeFunctionObject("getFullYear", (ctx, owner, args) => { return DateTime.Year; });
			this["getMonth"] = new NativeFunctionObject("getMonth", (ctx, owner, args) => { return DateTime.Month; });
			this["getDate"] = new NativeFunctionObject("getDate", (ctx, owner, args) => { return DateTime.Day; });
			this["getDay"] = new NativeFunctionObject("getDay", (ctx, owner, args) => { return (int)DateTime.DayOfWeek; });
			this["getHours"] = new NativeFunctionObject("getHours", (ctx, owner, args) => { return DateTime.Hour; });
			this["getMinutes"] = new NativeFunctionObject("getMinutes", (ctx, owner, args) => { return DateTime.Minute; });
			this["getSeconds"] = new NativeFunctionObject("getSeconds", (ctx, owner, args) => { return DateTime.Second; });
			this["getMilliseconds"] = new NativeFunctionObject("getMilliseconds", (ctx, owner, args) => { return DateTime.Millisecond; });
			this["getTime"] = new NativeFunctionObject("getTime", (ctx, owner, args) => { return Ticks; });
		}

		public DateObject() :
			this(DateTime.Now)
		{
		}

		public double Ticks
		{
			get
			{
				return (DateTime.ToUniversalTime().Ticks - StartTimeTicks) / 10000d;
			}
		}

		public override string ToString()
		{
			return DateTime.ToLongDateString();
		}
	}
}
