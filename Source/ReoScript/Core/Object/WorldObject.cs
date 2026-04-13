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
using System.Text;

namespace unvell.ReoScript
{
	#region World Value
	internal class WorldObject : ObjectValue
	{
		#region Built-in functions
		private static readonly NativeFunctionObject __stdin__ = new NativeFunctionObject("__stdin__", (ctx, owner, args) =>
		{
			return ctx.Srm.StandardInputProvider.Read();
		});

		private static readonly NativeFunctionObject __stdinln__ = new NativeFunctionObject("__stdinln__", (ctx, owner, args) =>
		{
			return ctx.Srm.StandardInputProvider.ReadLine();
		});

		private static readonly NativeFunctionObject __stdout__ = new NativeFunctionObject("__stdout__", (ctx, owner, args) =>
		{
			if (args == null || args.Length == 0)
			{
				ctx.Srm.StandardIOWrite(0);
			}
			else
			{
				//ctx.Srm.StandardIOWrite(args[0] == null ? ScriptRunningMachine.KEY_UNDEFINED : Convert.ToString(args[0]));
				ctx.Srm.StandardIOWrite(args[0]);
			}

			if (args.Length > 1)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 1; i < args.Length; i++)
				{
					sb.Append(' ');
					sb.Append(args[0] == null ? ScriptRunningMachine.KEY_UNDEFINED : Convert.ToString(args[i]));
				}

				ctx.Srm.StandardIOWrite(sb.ToString());
			}

			return null;
		});

		private static readonly NativeFunctionObject __stdoutln__ = new NativeFunctionObject("__stdoutln__", (ctx, owner, args) =>
		{
			if (args == null || args.Length == 0 || (args.Length == 1 && args[0] == null))
			{
				ctx.Srm.StandardIOWriteLine(string.Empty);
			}
			else
			{
				ctx.Srm.StandardIOWriteLine(args[0] == null ? ScriptRunningMachine.KEY_UNDEFINED : Convert.ToString(args[0]));
			}

			if (args.Length > 1)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 1; i < args.Length; i++)
				{
					sb.Append(' ');
					sb.Append(args[0] == null ? ScriptRunningMachine.KEY_UNDEFINED : Convert.ToString(args[i]));
				}

				ctx.Srm.StandardIOWriteLine(sb.ToString());
			}

			return null;
		});

		private static readonly NativeFunctionObject __parseInt__ = new NativeFunctionObject("parseInt", (ctx, owner, args) =>
		{
			if (args.Length == 0) return 0;
			else if (args.Length == 1)
			{
				if (args[0] is double) return Convert.ToInt32(args[0]);

				int i = 0;
				int.TryParse(Convert.ToString(args[0]), out i);
				return i;
			}
			else
			{
				try
				{
					return Convert.ToInt32(Convert.ToString(args[0]), Convert.ToInt32(args[1]));
				}
				catch
				{
					return 0;
				}
			}
		});

		private static readonly NativeFunctionObject __isNumber__ = new NativeFunctionObject("isNumber",
			(ctx, owner, args) =>
			{
				if (args.Length == 1)
				{
					return ScriptRunningMachine.TryGetNumberValue(args[0], out double val);
				}
				else
					return true;
			});

		private static readonly NativeFunctionObject __isNaN__ = new NativeFunctionObject("isNaN",
			(ctx, owner, args) =>
			{
				if (args.Length == 1)
				{
					return !ScriptRunningMachine.TryGetNumberValue(args[0], out double val);
				}
				else
					return true;
			});
		#endregion

		public WorldObject()
		{
			//this.Name = "Script";

			// built-in native functions
			this[__stdin__.FunName] = __stdin__;
			this[__stdinln__.FunName] = __stdinln__;
			this[__stdout__.FunName] = __stdout__;
			this[__stdoutln__.FunName] = __stdoutln__;
			this[__parseInt__.FunName] = __parseInt__;
			this[__isNumber__.FunName] = __isNumber__;
			this[__isNaN__.FunName] = __isNaN__;
		}
	}
	#endregion
}
