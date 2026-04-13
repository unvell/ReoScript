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
	#region Math
	class MathObject : ObjectValue
	{
		private static readonly Random rand = new Random();

		public MathObject()
		{
			#region random
			this["random"] = new NativeFunctionObject("random", (ctx, owner, args) =>
			{
				return rand.NextDouble();
			});
			#endregion // random
			#region round
			this["round"] = new NativeFunctionObject("round", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else if (args.Length < 2)
					return (Math.Round(ScriptRunningMachine.GetNumberValue(args[0])));
				else
					return (Math.Round(ScriptRunningMachine.GetNumberValue(args[0]),
						ScriptRunningMachine.GetIntValue(args[1])));
			});
			#endregion // round
			#region floor
			this["floor"] = new NativeFunctionObject("floor", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return (Math.Floor(ScriptRunningMachine.GetNumberValue(args[0])));
			});
			#endregion // floor

			#region sin
			this["sin"] = new NativeFunctionObject("sin", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Sin(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // sin
			#region cos
			this["cos"] = new NativeFunctionObject("cos", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Cos(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // ocs
			#region tan
			this["tan"] = new NativeFunctionObject("tan", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Tan(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // tan
			#region atan
			this["atan"] = new NativeFunctionObject("atan", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Atan(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // atan
			#region atan2
			this["atan2"] = new NativeFunctionObject("atan2", (ctx, owner, args) =>
			{
				if (args.Length < 2)
					return NaNValue.Value;
				else
					return Math.Atan2(ScriptRunningMachine.GetNumberValue(args[0], 0),
						ScriptRunningMachine.GetNumberValue(args[1], 0));
			});
			#endregion // atan2

			#region abs
			this["abs"] = new NativeFunctionObject("abs", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Abs(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // abs
			#region exp
			this["exp"] = new NativeFunctionObject("exp", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Exp(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // exp
			#region log
			this["log"] = new NativeFunctionObject("log", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Log(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // log
			#region cosh
			this["cosh"] = new NativeFunctionObject("cosh", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Cosh(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // log
			#region pow
			this["pow"] = new NativeFunctionObject("pow", (ctx, owner, args) =>
			{
				if (args.Length < 2)
					return NaNValue.Value;
				else
					return Math.Pow(ScriptRunningMachine.GetNumberValue(args[0], 0),
						ScriptRunningMachine.GetNumberValue(args[1], 0));
			});
			#endregion //pow
			#region sqrt
			this["sqrt"] = new NativeFunctionObject("sqrt", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Sqrt(ScriptRunningMachine.GetNumberValue(args[0], 0));
			});
			#endregion // sqrt
		}
	}
	#endregion
}
