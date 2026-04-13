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
using System.Reflection;

using unvell.ReoScript.Core.Statement;

namespace unvell.ReoScript
{
	static class PropertyAccessHelper
	{
		internal static void SetProperty(ScriptContext context, object target, string identifier, object value)
		{
			ScriptRunningMachine srm = context.Srm;

			if (target is ObjectValue)
			{
				ObjectValue objValue = (ObjectValue)target;

				object val = objValue[identifier];

				if (val is ExternalProperty)
				{
					((ExternalProperty)val).SetNativeValue(value);
				}
				else
				{
					objValue[identifier] = value;
				}
			}
			else if (target is IDictionary<string, object>)
			{
				IDictionary<string, object> dict = (IDictionary<string, object>)target;
				dict[identifier] = value;
			}
			else if (srm.AllowDirectAccess && !(target is ISyntaxTreeReturn))
			{
				string memberName = ScriptRunningMachine.GetNativeIdentifier(identifier);

				// if value is anonymous function, try to attach CLR event
				if (value is FunctionObject)
				{
					if (srm.AllowCLREvent)
					{
						EventInfo ei = target.GetType().GetEvent(memberName);
						if (ei != null)
						{
							srm.AttachEvent(context, target, ei, value as FunctionObject);

							if (target is ObjectValue)
							{
								((ObjectValue)target)[identifier] = value;
							}
						}
					}
				}
				else
				{
					PropertyInfo pi = target.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);

					if (pi != null)
					{
						try
						{
							pi.SetValue(target, srm.ConvertToCLRType(context, value, pi.PropertyType), null);
						}
						catch (Exception ex)
						{
							if (srm.IgnoreCLRExceptions)
							{
								// call error, do nothing
							}
							else
								throw ex;
						}
					}
					else
					{
						FieldInfo fi = target.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public);

						if (fi != null)
						{
							try
							{
								fi.SetValue(target, value);
							}
							catch (Exception ex)
							{
								if (srm.IgnoreCLRExceptions)
								{
									// call error, do nothing
								}
								else
									throw ex;
							}
						}
						else
						{
							// remove event if property value is set to null
							if (value == null && srm.AllowCLREvent)
							{
								EventInfo ei = target.GetType().GetEvent(memberName, BindingFlags.Instance | BindingFlags.Public);

								if (ei != null)
								{
									srm.DetachEvent(target, ei);
								}
							}

							if (target is ObjectValue)
							{
								((ObjectValue)target)[identifier] = value;
							}
							else
							{
								// can not found property or field, ignore this access
							}
						}
					}
				}
			}
			else
			{
				// unknown type, ignore it
			}
		}

		internal static object GetProperty(ScriptContext ctx, object target, string identifier)
		{
			ScriptRunningMachine srm = ctx.Srm;

			if (target is string str)
			{
				// FIXME: not very good to get property 'length' in hard coding
				if (identifier == "length")
					return str.Length;
				else
					return PropertyAccessHelper.GetProperty(ctx, srm.BuiltinConstructors.StringFunction[
						ScriptRunningMachine.KEY_PROTOTYPE], identifier);
			}
			else if (ScriptRunningMachine.IsPrimitiveNumber(target))
			{
				return PropertyAccessHelper.GetProperty(ctx, srm.BuiltinConstructors.NumberFunction[
					ScriptRunningMachine.KEY_PROTOTYPE], identifier);
			}
			else if (target is bool)
			{
				return PropertyAccessHelper.GetProperty(ctx, srm.BuiltinConstructors.BooleanFunction[
					ScriptRunningMachine.KEY_PROTOTYPE], identifier);
			}
			else if (target is IList listObj && !(target is ArrayObject))
			{
				// FIXME: not very good to get property 'length' in hard coding
				if (identifier == "length")
				{
					return listObj.Count;
				}
				else
				{
					return PropertyAccessHelper.GetProperty(ctx, srm.BuiltinConstructors.ArrayFunction[
						ScriptRunningMachine.KEY_PROTOTYPE], identifier);
				}
			}
			else if (target is ObjectValue objValue)
			{
				object val = objValue[identifier];

				if (val is ExternalProperty extProp)
				{
					return extProp.GetNativeValue();
				}
				else
				{
					// if value is not found, get property from its prototype
					if (val == null && objValue.HasOwnProperty(ScriptRunningMachine.KEY___PROTO__))
					{
						val = PropertyAccessHelper.GetProperty(ctx,
							objValue.GetOwnProperty(ScriptRunningMachine.KEY___PROTO__), identifier);
					}

					return val;
				}
			}
			else if (target is IDictionary<string, object>)
			{
				IDictionary<string, object> dict = (IDictionary<string, object>)target;
				object o = null;
				dict.TryGetValue(identifier, out o);
				return o;
			}
			else if (srm.AllowDirectAccess && !(target is ISyntaxTreeReturn))
			{
				string memberName = ((srm.WorkMode & MachineWorkMode.AutoUppercaseWhenCLRCalling)
					== MachineWorkMode.AutoUppercaseWhenCLRCalling)
					? ScriptRunningMachine.GetNativeIdentifier(identifier) : identifier;

				PropertyInfo pi = target.GetType().GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);

				if (pi != null)
				{
					try
					{
						object returnObj = pi.GetValue(target, null);

						if (srm.AutoImportRelationType)
						{
							srm.ImportType(returnObj.GetType());
						}

						return returnObj;
					}
					catch (Exception ex)
					{
						if (srm.IgnoreCLRExceptions)
						{
							// call error, return undefined
							return null;
						}
						else
							throw ex;
					}
				}
				else
				{
					FieldInfo fi = target.GetType().GetField(memberName, BindingFlags.Public | BindingFlags.Instance);

					if (fi != null)
					{
						try
						{
							return fi.GetValue(target);
						}
						catch (Exception ex)
						{
							if (srm.IgnoreCLRExceptions)
							{
								// call error, return undefined
								return null;
							}
							else
								throw ex;
						}
					}
					else
					{
						EventInfo ei = target.GetType().GetEvent(memberName, BindingFlags.Public | BindingFlags.Instance);
						if (ei != null)
						{
							object attachedEventFun = srm.GetAttachedEvent(target, ei);

							// synchronize registed event and property of object
							if (target is ObjectValue)
							{
								((ObjectValue)target)[identifier] = attachedEventFun;
							}

							return attachedEventFun;
						}
						else if (target is ObjectValue)
						{
							return ((ObjectValue)target)[identifier];
						}
					}
				}
			}

			return null;
		}
	}
}
