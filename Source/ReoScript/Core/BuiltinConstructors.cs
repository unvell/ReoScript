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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using unvell.ReoScript.Core;
using unvell.ReoScript.Core.Statement;
using unvell.ReoScript.Parsers;

namespace unvell.ReoScript
{
	internal class BuiltinConstructors
	{
		internal ObjectConstructorFunction ObjectFunction;
		internal StringConstructorFunction StringFunction;
		internal ArrayConstructorFunction ArrayFunction;
		internal TypedNativeFunctionObject FunctionFunction;
		internal TypedNativeFunctionObject NumberFunction;
		internal TypedNativeFunctionObject DateFunction;
		internal ErrorConstructorFunction ErrorFunction;
		internal NativeFunctionObject BooleanFunction;

		public BuiltinConstructors()
		{
			ObjectFunction = new ObjectConstructorFunction();
			StringFunction = new StringConstructorFunction();

			#region Function
			FunctionFunction = new TypedNativeFunctionObject
				(typeof(FunctionObject), "Function", (ctx, owner, args) =>
				{
					FunctionObject fun = owner as FunctionObject;
					//TOOD: create function from string
					if (fun == null) fun = ctx.CreateNewObject(FunctionFunction, false) as FunctionObject;
					return fun;
				}, (proto) =>
				{
					proto["call"] = new NativeFunctionObject("call", (ctx, owner, args) =>
					{
						AbstractFunctionObject func = owner as AbstractFunctionObject;
						if (func != null)
						{
							object[] callArgs = null;

							if (args.Length > 1)
							{
								callArgs = new object[args.Length - 1];
								Array.Copy(args, 1, callArgs, 0, args.Length - 1);
							}

							return ctx.Srm.InvokeFunction(ctx, args.Length > 0 ? args[0] : null, func, callArgs);
						}
						return null;
					});

					proto["apply"] = new NativeFunctionObject("apply", (ctx, owner, args) =>
					{
						AbstractFunctionObject func = owner as AbstractFunctionObject;
						if (func != null)
						{
							List<object> callArgs = new List<object>();

							if (args.Length > 1 && args[1] is IEnumerable argEnum)
							{
								foreach (var arg in argEnum) {
									callArgs.Add(arg);
								}
							}

							return ctx.Srm.InvokeFunction(ctx, args.Length > 0 ? args[0] : null, func,  callArgs.ToArray());
						}
						return null;
					});
				});
			#endregion

			#region Number
			NumberFunction = new TypedNativeFunctionObject
				(typeof(NumberObject), "Number", (ctx, owner, args) =>
				{
					if (args == null || args.Length <= 0)
					{
						return 0;
					}

					double num = 0;
					if (double.TryParse(Convert.ToString(args[0]), out num))
					{
						return num;
					}
					else
					{
						return NaNValue.Value;
					}
				}, (proto) =>
				{
					proto["toString"] = new NativeFunctionObject("toString", (ctx, owner, args) =>
					{
						int radix = 10;

						if (args != null && args.Length > 0)
						{
							radix = ScriptRunningMachine.GetIntParam(args, 0);
						}

						double num = 0;

						if (ScriptRunningMachine.IsPrimitiveNumber(owner))
						{
							num = ScriptRunningMachine.GetNumberValue(owner);
						}
						else if (owner is NumberObject)
						{
							num = ((NumberObject)owner).Number;
						}

						try
						{
							return Convert.ToString((int)num, radix);
						}
						catch
						{
							throw new ReoScriptRuntimeException("Number.toString radix argument must between 2 and 36.");
						}
					});
				});
			#endregion

			#region Date
			DateFunction = new TypedNativeFunctionObject
				(typeof(DateObject), "Date", (ctx, owner, args) =>
				{
					DateObject dateObj = owner as DateObject;
					if (dateObj == null) dateObj = ctx.CreateNewObject(DateFunction, false) as DateObject;
					return dateObj;
				}, (obj) =>
				{
					if (obj is ObjectValue)
					{
						ObjectValue proto = obj as ObjectValue;
						proto["subtract"] = new NativeFunctionObject("subtract", (ctx, owner, args) =>
						{
							if (args.Length < 1 || !(args[0] is DateObject) || !(owner is DateObject)) return NaNValue.Value;

							DateObject ownerProto = (DateObject)owner;
							return ownerProto.DateTime.Subtract(((DateObject)args[0]).DateTime).TotalMilliseconds;
						});
					}
				});
			#endregion

			#region Array
			ArrayFunction = new ArrayConstructorFunction();
			#endregion

			#region Error
			ErrorFunction = new ErrorConstructorFunction();
			#endregion

			#region Boolean
			BooleanFunction = new TypedNativeFunctionObject<BooleanObject>("Boolean", (ctx, owner, args) =>
			{
				if (args.Length == 0)
				{
					return false;
				}
				else
				{
					object obj = args[0];

					return ((obj is bool) && ((bool)obj));
				}
			}, (protoObj) =>
			{
				if (protoObj is ObjectValue)
				{
					ObjectValue proto = (ObjectValue)protoObj;

					proto["valueOf"] = new NativeFunctionObject("valueOf", (ctx, owner, args) =>
					{
						if (owner is BooleanObject)
						{
							return ((BooleanObject)owner).Boolean;
						}
						else
						{
							return false;
						}
					});
				}
			});
			#endregion
		}

		#region Internal Global Functions
		private static readonly NativeFunctionObject __setTimeout__ = new NativeFunctionObject("setTimeout", (ctx, owner, args) =>
		{
			if (args.Length < 2) return 0;
			int interval = ScriptRunningMachine.GetIntParam(args, 1, 1000);
			object[] callArgs = null;
			if (args != null && args.Length > 2)
			{
				callArgs = new object[args.Length - 2];
				Array.Copy(args, 2, callArgs, 0, args.Length - 2);
			}
			return ctx.Srm.AsyncCall(args[0], interval, false, callArgs);
		});

		private static readonly NativeFunctionObject __setInterval__ = new NativeFunctionObject("setInterval", (ctx, owner, args) =>
		{
			if (args.Length < 2) return 0;
			int interval = ScriptRunningMachine.GetIntParam(args, 1, 1000);
			object[] callArgs = null;
			if (args != null && args.Length > 2)
			{
				callArgs = new object[args.Length - 2];
				Array.Copy(args, 2, callArgs, 0, args.Length - 2);
			}
			return ctx.Srm.AsyncCall(args[0], interval, true, callArgs);
		});

		private static readonly NativeFunctionObject __clearTimeout__ = new NativeFunctionObject("clearTimeout", (ctx, owner, args) =>
		{
			if (args.Length < 1) return 0;
			long id = ScriptRunningMachine.GetLongParam(args, 0, 0);
			return ctx.Srm.CancelAsyncCall(id);
		});

		private static readonly NativeFunctionObject __alert__ = new NativeFunctionObject("alert", (ctx, owner, args) =>
		{
			if (args.Length > 0)
			{
				ctx.Srm.StandardIOWriteLine(Convert.ToString(args[0]));
			}
			return null;
		});

		private static readonly NativeFunctionObject __confirm__ = new NativeFunctionObject("confirm", (ctx, owner, args) =>
		{
			if (args.Length > 0)
			{
				ctx.Srm.StandardIOWriteLine(Convert.ToString(args[0]));
			}
			// Cross-platform: always returns true (no GUI dialog available)
			return true;
		});

		private static readonly NativeFunctionObject __eval__ = new NativeFunctionObject("eval", (ctx, owner, args) =>
		{
			if (args.Length == 0) return false;
			return ctx.Srm.CalcExpression(Convert.ToString(args[0]), ctx);
		});
		#endregion

		public void ApplyToScriptRunningMachine(ScriptRunningMachine srm)
		{
			if (srm != null && srm.GlobalObject != null)
			{
				// built-in object constructors
				srm.SetGlobalVariable(ObjectFunction.FunName, ObjectFunction);
				srm.SetGlobalVariable(FunctionFunction.FunName, FunctionFunction);
				srm.SetGlobalVariable(ErrorFunction.FunName, ErrorFunction);
				srm.SetGlobalVariable(StringFunction.FunName, StringFunction);
				srm.SetGlobalVariable(NumberFunction.FunName, NumberFunction);
				srm.SetGlobalVariable(DateFunction.FunName, DateFunction);
				srm.SetGlobalVariable(ArrayFunction.FunName, ArrayFunction);
				srm.SetGlobalVariable(BooleanFunction.FunName, BooleanFunction);

				// built-in objects
				srm.SetGlobalVariable("Math", new MathObject());

				if ((srm.CoreFeatures & CoreFeatures.Console) == CoreFeatures.Console)
					srm.GlobalObject["console"] = srm.CreateNewObject();

				if ((srm.CoreFeatures & CoreFeatures.Eval) == CoreFeatures.Eval)
					srm.GlobalObject[__eval__.FunName] = __eval__;

				// importModule() — always available
				srm.GlobalObject["importModule"] = new NativeFunctionObject("importModule", (ctx, owner, args) =>
				{
					if (args.Length == 0 || args[0] == null)
						throw new ReoScriptRuntimeException("importModule requires a file path argument.");

					string codeFile = Convert.ToString(args[0]);
					string path = Path.GetFullPath(Path.Combine(
						string.IsNullOrEmpty(ctx.SourceFilePath) ? ctx.Srm.WorkPath
						: Path.GetDirectoryName(ctx.SourceFilePath), codeFile));

					return ctx.Srm.ImportModuleFile(path);
				});

				if ((srm.CoreFeatures & CoreFeatures.AsyncCalling) == CoreFeatures.AsyncCalling)
				{
					srm.GlobalObject[__setTimeout__.FunName] = __setTimeout__;
					srm.GlobalObject[__clearTimeout__.FunName] = __clearTimeout__;
				}

				if ((srm.CoreFeatures & CoreFeatures.AsyncCalling) == CoreFeatures.AsyncCalling)
				{
					srm.GlobalObject[__setInterval__.FunName] = __setInterval__;
					srm.GlobalObject["clearInterval"] = __clearTimeout__;
				}

				if ((srm.CoreFeatures & CoreFeatures.Alert) == CoreFeatures.Alert)
				{
					srm.GlobalObject[__alert__.FunName] = __alert__;
					srm.GlobalObject[__confirm__.FunName] = __confirm__;
				}

				#region JSON
				if ((srm.CoreFeatures & CoreFeatures.JSON) == CoreFeatures.JSON)
				{
					ObjectValue json = new ObjectValue()
					{
						//Name = "JSON",
					};

					json["parse"] = new NativeFunctionObject("parse", (ctx, owner, args) =>
					{
						if (args.Length == 0) return null;

						string lit = Convert.ToString(args[0]);
						if (string.IsNullOrEmpty(lit)) return null;

						// Parse JSON as a ReoScript object literal expression
						var jsonParser = new ReoScriptHandwrittenParser();
						SyntaxNode jsonTree = jsonParser.ParseExpression(lit.Trim());
						object parsed = ScriptRunningMachine.ParseNode(jsonTree, ctx);
						if (parsed is IAccess acc) parsed = acc.Get();

						if (args.Length >= 2 && args[1] is AbstractFunctionObject func)
						{
							// Apply reviver function to each key/value pair
							if (parsed is ObjectValue ov)
							{
								foreach (string key in ov)
								{
									ov[key] = srm.InvokeAbstractFunction(ctx.ThisObject, func, new object[] { key, ov[key] });
								}
							}
						}

						return parsed;
					});

					json["stringify"] = new NativeFunctionObject("stringify", (ctx, owner, args) =>
					{
						if (args.Length == 0 || args[0] == null)
						{
							return string.Empty; // FIXME: StringObject ?
						}

						AbstractFunctionObject func = null;
						if (args.Length >= 2 && args[1] is AbstractFunctionObject)
						{
							func = ((args[1]) as AbstractFunctionObject);
						}

						return ScriptRunningMachine.ConvertToJSONString(args[0], (key, value) =>
						{
							return func == null ? value : srm.InvokeAbstractFunction(ctx.ThisObject, func, new object[] { key, value });
						}, srm.AllowDirectAccess);
					});

					srm.SetGlobalVariable("JSON", json);
				}
				#endregion
			}
		}
	}
}
