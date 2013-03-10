///////////////////////////////////////////////////////////////////////////////
// 
// ReoScript
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
// Copyright (C) unvell.com, 2013. All Rights Reserved
//
///////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Net;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;

using Antlr.Runtime;
using Antlr.Runtime.Tree;

using Unvell.ReoScript.Properties;
using System.Reflection.Emit;

namespace Unvell.ReoScript
{
	public partial class ReoScriptLexer
	{
		public static readonly int HIDDEN = Hidden;
	}

	#region Common Tree Return Value
	public interface ISyntaxTreeReturn { }
	#endregion

	#region Value
	public abstract class Value : ISyntaxTreeReturn
	{
	}
	#endregion
	#region NaNValue
	public sealed class NaNValue : Value
	{
		public static readonly NaNValue Value = new NaNValue();
		private NaNValue() { }
		public override string ToString()
		{
			return "NaN";
		}
	}
	public sealed class InfinityValue : Value
	{
		public static readonly InfinityValue Value = new InfinityValue();
		private InfinityValue() { }
		public override string ToString()
		{
			return "Infinity";
		}
	}
	public sealed class MinusInfinityValue : Value
	{
		public static readonly MinusInfinityValue Value = new MinusInfinityValue();
		private MinusInfinityValue() { }
		public override string ToString()
		{
			return "-Infinity";
		}
	}
	#endregion

	#region NumberObject
	public class NumberObject : ObjectValue
	{
		public double Number { get; set; }
		public NumberObject() : this(0) { }
		public NumberObject(double num)
		{
			this.Number = num;
		}
		public override string Name { get { return "Number"; } }
	}
	#endregion
	#region DateTimeValue
	public class DateTimeValue : ObjectValue
	{
		private DateTime dt;

		public DateTimeValue(DateTime value)
		{
			this.dt = value;
		}
	}
	#endregion
	#region StringValue
	public class StringObject : ObjectValue
	{
		public string String { get; set; }

		public StringObject()
		{
			this["trim"] = new NativeFunctionValue("trim", (srm, owner, args) =>
			{
				return new StringObject(((StringObject)owner).String = Convert.ToString(owner).Trim());
			});

			this["indexOf"] = new NativeFunctionValue("indexOf", (srm, owner, args) =>
			{
				return args.Length == 0 ? -1 : Convert.ToString(owner).IndexOf(Convert.ToString(args[0]));
			});

			this["startsWith"] = new NativeFunctionValue("startsWith", (srm, owner, args) =>
			{
				return args.Length == 0 ? false : Convert.ToString(owner).StartsWith(Convert.ToString(args[0]));
			});

			this["endsWith"] = new NativeFunctionValue("endWith", (srm, owner, args) =>
			{
				return args.Length == 0 ? false : Convert.ToString(owner).EndsWith(Convert.ToString(args[0]));
			});

			this["length"] = new ExternalProperty(() => { return String.Length; }, (v) => { });

			this["repeat"] = new NativeFunctionValue("repeat", (srm, owner, args) =>
			{
				int count = ScriptRunningMachine.GetIntParam(args, 0, 0);

				if (count > 0)
				{
					string str = ((StringObject)owner).String;
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < count; i++) sb.Append(str);
					return sb.ToString();
				}
				else
					return new StringObject();
			});

			this["join"] = new NativeFunctionValue("join", (srm, owner, args) =>
			{
				return new StringObject();
			});
		}
		public StringObject(string text)
			: this()
		{
			String = text;
		}
		public override bool Equals(object obj)
		{
			return (String == null && obj == null) ? false : String.Equals(Convert.ToString(obj));
		}
		public override string ToString()
		{
			return String;
		}
		public override string Name { get { return "String"; } }
	}
	#endregion
	#region Object Value
	public class ObjectValue : Value, IEnumerable
	{
		public ObjectValue()
		{
			Members = new Dictionary<string, object>();
		}

		private Dictionary<string, object> Members { get; set; }

		public virtual object this[string identifier]
		{
			get
			{
				return Members.ContainsKey(identifier) ? Members[identifier] : null;
			}
			set
			{
				Members[identifier] = value;
			}
		}

		public bool DeleteMember(string identifier)
		{
			if (Members.ContainsKey(identifier))
			{
				Members.Remove(identifier);
				return true;
			}
			else
			{
				return false;
			}
		}

		public virtual string Name { get { return "Object"; } }

		public override string ToString()
		{
			return string.Format("[object {0}]", Name);
			//return DumpObject("Object");
		}

		public string DumpObject()
		{
			if (Members.Count > 0)
			{
				StringBuilder sb = new StringBuilder();
				if (this is ArrayObject || this is StringObject || this is NumberObject)
				{
					sb.AppendLine(string.Format("[object {0}: {1}]", Name, this.ToString()));
				}
				else
				{
					sb.AppendLine(string.Format("[object {0}]", Name));
				}

				foreach (string name in Members.Keys)
				{
					object val = Members[name];

					sb.AppendLine(string.Format("  {0,-20}: {1}", name, Convert.ToString(val)));
				}

				return sb.ToString();
			}
			else
				return Name;
		}

		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return Members.Keys.GetEnumerator();
		}

		#endregion
	}
	#endregion
	#region Extension Values
	/// <summary>
	/// Dynamic access an object with property name
	/// </summary>
	public class DynamicPropertyObject : ObjectValue
	{
		public Action<object, object> propertySetter { get; set; }
		public Func<object> propertyGetter { get; set; }

		public DynamicPropertyObject(Action<object, object> setter, Func<object> getter)
		{
			this.propertySetter = setter;
			this.propertyGetter = getter;
		}

		public override object this[string name]
		{
			get
			{
				return propertyGetter != null ? propertyGetter() : base[name];
			}
			set
			{
				if (propertySetter != null)
				{
					propertySetter(name, value);
				}
				else
				{
					base[name] = value;
				}
			}
		}
	}

	/// <summary>
	/// ExternalProperty class provides an interface to extend a property to an 
	/// object which declared and used in ReoScript context. ExternalProperty 
	/// has a getter and setter delegate method that will be invoked automatically
	/// when the property value is accessed in script at runtime.
	/// </summary>
	public class ExternalProperty : ISyntaxTreeReturn
	{
		/// <summary>
		/// Getter method will be invoked when value is required in script
		/// </summary>
		public Func<object> Getter { get; set; }

		/// <summary>
		/// Setter method will be invoked when property is set to a value 
		/// in script.
		/// </summary>
		public Action<object> Setter { get; set; }

		/// <summary>
		/// Create instance with getter and setter method
		/// </summary>
		/// <param name="getter">method invoked when value be set </param>
		/// <param name="setter"></param>
		public ExternalProperty(Func<object> getter, Action<object> setter)
		{
			this.Getter = getter;
			this.Setter = setter;
		}

		/// <summary>
		/// Invoke getter to retrieve a value from CLR runtime
		/// </summary>
		/// <returns></returns>
		public object GetNativeValue()
		{
			return Getter == null ? null : Getter();
		}

		/// <summary>
		/// Invoke setter to set property value that provided from CLR
		/// </summary>
		/// <param name="value"></param>
		public void SetNativeValue(object value)
		{
			if (Setter != null) Setter(value);
		}

		/// <summary>
		/// Convert value to string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Getter == null ? "null" : Convert.ToString(Getter());
		}
	}

	/// <summary>
	/// IDirectAccess is a empty interface that allows CLR instance can be 
	/// accessed directly from ReoScript.
	/// </summary>
	//public interface IDirectAccess
	//{
	//}

	/// <summary>
	/// DirectAccessAttribute allows CLR instace can be accessed directly 
	/// from ReoScript.
	/// </summary>
	//public class DirectAccessAttribute : Attribute
	//{
	//}

	#endregion // Extension Values
	#region FunctionValue
	public abstract class AbstractFunctionValue : ObjectValue
	{
		public new abstract string Name { get; set; }
		public virtual object CreateObject(ScriptRunningMachine srm, CommonTree argTree) { return null; }
	}
	public class FunctionValue : AbstractFunctionValue
	{
		public override string Name { get; set; }
		public string[] Args { get; set; }
		public CommonTree Body { get; set; }

		public override string ToString()
		{
			return "function " + Name + "() { ... }";
		}
	}
	public class NativeFunctionValue : AbstractFunctionValue
	{
		public override string Name { get; set; }

		public override string ToString()
		{
			return "function " + Name + "() { [native code] }";
		}

		public NativeFunctionValue(string name, Func<ScriptRunningMachine, object, object[], object> body)
		{
			this.Name = name;
			this.Body = body;
		}

		public Func<ScriptRunningMachine, object, object[], object> Body { get; set; }
	}
	public class TypedNativeFunctionValue : NativeFunctionValue
	{
		public Type Type { get; set; }

		public TypedNativeFunctionValue(Type type, string name, Func<ScriptRunningMachine, object, object[], object> body)
			: base(name, body)
		{
			this.Type = type;
		}

		public override object CreateObject(ScriptRunningMachine srm, CommonTree argTree)
		{
			try
			{
				object[] args = srm.GetParameterList(argTree);

				ConstructorInfo ci = this.Type.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == args.Length);

				if (ci == null)
				{
					throw new AWDLRuntimeException("Cannot create .Net object with incorrect parameters.");
				}

				object[] cargs = new object[args.Length];

				ParameterInfo[] pis = ci.GetParameters();
				for (int i = 0; i < args.Length; i++)
				{
					cargs[i] = srm.ConvertToCLRType(args[i], pis[i].ParameterType);
				}

				return System.Activator.CreateInstance(this.Type, BindingFlags.Default, null, cargs, null);
			}
			catch (Exception ex)
			{
				throw new AWDLRuntimeException("Error to create .Net instance: " + this.Type.ToString(), ex);
			}
		}
	}
	#endregion

	#region Built-in Objects
	#region World Value
	internal class WorldValue : ObjectValue
	{
		public static readonly string GlobalInstanceName = "script";

		private static readonly NativeFunctionValue __stdout__ = new NativeFunctionValue("__stdout__", (srm, owner, args) =>
		{
			if (args.Length == 0)
			{
				srm.StdOutputWrite(string.Empty);
			}
			else
			{
				srm.StdOutputWrite(args[0] == null ? ScriptRunningMachine.UNDEFINED : Convert.ToString(args[0]));
			}

			if (args.Length > 1)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 1; i < args.Length; i++)
				{
					sb.Append(' ');
					sb.Append(args[0] == null ? ScriptRunningMachine.UNDEFINED : Convert.ToString(args[i]));
				}

				srm.StdOutputWrite(sb.ToString());
			}

			return null;
		});

		private static readonly NativeFunctionValue __setTimeout__ = new NativeFunctionValue("setTimeout", (srm, owner, args) =>
		{
			if (args.Length < 2) return 0;

			int interval = ScriptRunningMachine.GetIntParam(args, 1, 1000);

			srm.CallTimeout(args[0], interval);

			return 1; // TODO: thread id
		});

		private static readonly NativeFunctionValue __alert__ = new NativeFunctionValue("alert", (srm, owner, args) =>
		{
			if (args.Length > 0)
			{
				MessageBox.Show(Convert.ToString(args[0]));
			}
			return null;
		});

		private static readonly NativeFunctionValue __eval__ = new NativeFunctionValue("eval", (srm, owner, args) =>
		{
			if (args.Length == 0) return false;
			return srm.CalcExpression(Convert.ToString(args[0]));
		});

		private static readonly NativeFunctionValue __parseInt__ = new NativeFunctionValue("parseInt", (srm, owner, args) =>
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

		#region built-in Object Constructors

		internal static readonly NativeFunctionValue ObjectFunction = new NativeFunctionValue("Object", (srm, owner, args) =>
		{
			return srm.CreateNewObject(ObjectFunction, false);
		});

		private static readonly TypedNativeFunctionValue StringFunction = new TypedNativeFunctionValue
			(typeof(StringObject), "String", (srm, owner, args) =>
		{
			StringObject str = srm.CreateNewObject(StringFunction, false) as StringObject;
			if (args.Length > 0) str.String = Convert.ToString(args[0]);
			return str;
		});

		private static readonly TypedNativeFunctionValue NumberFunction = new TypedNativeFunctionValue
			(typeof(NumberObject), "Number", (srm, owner, args) =>
		{
			NumberObject num = owner as NumberObject;
			if (num == null) num = srm.CreateNewObject(NumberFunction, false) as NumberObject;

			num["toString"] = new NativeFunctionValue("toString", (_srm, _owner, _args) =>
			{
				if (args.Length == 1 && Convert.ToInt32(args[0]) == 16)
				{
					try
					{
						return new StringObject(Convert.ToString(Convert.ToInt32(num), 16));
					}
					catch
					{
						return new StringObject(Convert.ToString(num));
					}
				}
				else
					return new StringObject(Convert.ToString(num));
			});

			if (args.Length > 0) num.Number = Convert.ToDouble(args[0]);
			return num;
		});
		#endregion

		public WorldValue()
		{
			// built-in native functions
			this[__stdout__.Name] = __stdout__;
			this[__eval__.Name] = __eval__;
			this[__setTimeout__.Name] = __setTimeout__;
			this[__alert__.Name] = __alert__;
			this[__parseInt__.Name] = __parseInt__;

			// built-in object constructors
			this[ObjectFunction.Name] = ObjectFunction;
			this[StringFunction.Name] = StringFunction;
			this[NumberFunction.Name] = NumberFunction;

			// built-in objects
			this["Math"] = new MathObject();
		}

		public override string Name { get { return "Script"; } }
	}
	#endregion

	#region Math
	class MathObject : ObjectValue
	{
		private static readonly Random rand = new Random();
		public MathObject()
		{
			this["random"] = new NativeFunctionValue("random", (srm, owner, args) =>
			{
				return rand.NextDouble();
			});

			this["round"] = new NativeFunctionValue("round", (srm, owner, args) =>
			{
				if (args.Length < 0)
					return NaNValue.Value;
				else if (args.Length < 2)
					return (Math.Round(Convert.ToDouble(args[0])));
				else
					return (Math.Round(Convert.ToDouble(args[0]), Convert.ToInt32(args[1])));
			});

			this["floor"] = new NativeFunctionValue("floor", (srm, owner, args) =>
			{
				if (args.Length < 0)
					return NaNValue.Value;
				else
					return (Math.Floor(Convert.ToDouble(args[0])));
			});

			this["min"] = new NativeFunctionValue("min", (srm, owner, args) =>
			{
				if (args.Length == 2)
					return Convert.ToDouble(args[0]) < Convert.ToDouble(args[1]) ? args[0] : args[1];
				else if (args.Length == 1)
					return args[0];
				else
					return InfinityValue.Value;
			});

			this["max"] = new NativeFunctionValue("max", (srm, owner, args) =>
			{
				if (args.Length == 2)
					return Convert.ToDouble(args[0]) > Convert.ToDouble(args[1]) ? args[0] : args[1];
				else if (args.Length == 1)
					return args[0];
				else
					return MinusInfinityValue.Value;
			});
		}
		public override string Name { get { return "Math"; } }
	}
	#endregion

	#region Array
	internal class ArrayObject : ObjectValue, IEnumerable
	{
		private ArrayList list = new ArrayList(5);

		public ArrayList List
		{
			get { return list; }
			set { list = value; }
		}

		public ArrayObject()
		{
			this["length"] = new ExternalProperty(
				() => { return list.Count; }, (v) =>
				{
					int len = (int)(double)v;
					if (len < list.Count)
					{
						list.RemoveRange(len, list.Count - len);
					}
					else
					{
						ArrayList newList = new ArrayList(len);
						newList.AddRange(list);
						list = newList;
					}
				});

			this["push"] = new NativeFunctionValue("push", (srm, owner, args) =>
			{
				foreach (object v in args)
				{
					((ArrayObject)owner).list.Add(v);
				}
				return null;
			});

			this["sort"] = new NativeFunctionValue("sort", (srm, owner, args) =>
			{
				((ArrayObject)owner).list.Sort();
				return null;
			});
		}
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(128);
			sb.Append('[');
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0) sb.Append(", ");
				object val = list[i];
				sb.Append(val == null ? ScriptRunningMachine.UNDEFINED : Convert.ToString(val));
			}
			sb.Append(']');
			return sb.ToString();
		}

		public override string Name { get { return "Array"; } }

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}

		#endregion
	}
	#endregion
	#endregion

	#region BreakNode
	class BreakNode : ISyntaxTreeReturn
	{
	}
	#endregion
	#region ContinueNode
	class ContinueNode : ISyntaxTreeReturn
	{
	}
	#endregion
	#region ReturnNode
	class ReturnNode : ISyntaxTreeReturn
	{
		public object Value { get; set; }

		public ReturnNode(object value)
		{
			this.Value = value;
		}
	}
	#endregion

	#region Exceptions
	#region AWDLException
	public class AWDLException : Exception
	{
		public AWDLException(string msg) : base(msg) { }
		public AWDLException(string msg, Exception inner) : base(msg, inner) { }
	}
	#endregion
	#region AWDLSyntaxErrorException
	class AWDLSyntaxErrorException : AWDLException
	{
		public AWDLSyntaxErrorException(string msg) : base(msg) { }
		public AWDLSyntaxErrorException(string msg, Exception inner) : base(msg, inner) { }
	}
	#endregion
	#region AWDLRuntimeException
	public class AWDLRuntimeException : AWDLException
	{
		public AWDLRuntimeException(string msg) : base(msg) { }
		public AWDLRuntimeException(string msg, Exception inner) : base(msg, inner) { }
	}
	#endregion AWDLRuntimeException
	#endregion

	#region Access
	interface IAccess : ISyntaxTreeReturn
	{
		void Set(object value);
		object Get();
	}
	abstract class AccessValue : ISyntaxTreeReturn, IAccess
	{
		protected ScriptRunningMachine srm;
		public AccessValue(ScriptRunningMachine srm)
		{
			this.srm = srm;
		}

		#region IAccess Members

		public abstract void Set(object value);
		public abstract object Get();

		#endregion
	}

	#region Variable Access
	class VariableAccess : AccessValue
	{
		private string identifier;
		public string Identifier
		{
			get { return identifier; }
			set { identifier = value; }
		}
		public VariableAccess(string identifier, ScriptRunningMachine srm)
			: base(srm)
		{
			this.identifier = identifier;
			Scope = srm.CurrentContext.GetVariableScope(identifier);

			if (Scope == null && srm.CurrentContext.GlobalObject[identifier] != null)
			{
				GlobalObject = srm.CurrentContext.GlobalObject;
			}
		}
		public CallScope Scope { get; set; }
		public ObjectValue GlobalObject { get; set; }
		#region Access Members
		public override void Set(object value)
		{
			if (Scope != null)
			{
				Scope[identifier] = value;
			}
			else if (GlobalObject != null)
			{
				GlobalObject[identifier] = value;
			}
			else
			{
				// new variable declare to global object 
				srm.CurrentContext[identifier] = value;
			}
		}
		public override object Get()
		{
			if (identifier == WorldValue.GlobalInstanceName)
			{
				return srm.CurrentContext.GlobalObject;
			}
			else
				return Scope == null ? (GlobalObject == null ? null : GlobalObject[identifier]) : Scope[identifier];
		}
		#endregion
	}
	#endregion
	#region Array Access
	class ArrayAccess : AccessValue
	{
		private object array;
		public object Array
		{
			get { return array; }
			set { array = value; }
		}
		private int index;
		public int Index
		{
			get { return index; }
			set { index = value; }
		}
		public ArrayAccess(object array, int index, ScriptRunningMachine srm)
			: base(srm)
		{
			this.array = array;
			this.index = index;
		}
		#region Access Members
		public override void Set(object value)
		{
			if ((array is ArrayObject))
			{
				((ArrayObject)array).List[index] = value;
			}
			else if (array is StringObject)
			{
				string str = ((StringObject)array).String;
				if (index < str.Length)
				{
					str = str.Substring(0, index) + Convert.ToString(value) + str.Substring(index + 1);
					((StringObject)array).String = str;
				}
				else
				{
					((StringObject)array).String += value;
				}
			}
			else if (srm.EnableDirectAccess && srm.IsDirectAccessObject(array))
			{
				if (array is IList)
				{
					((IList)array)[index] = value;
				}
			}
		}
		public override object Get()
		{
			if (array is ArrayObject)
			{
				return ((ArrayObject)array).List[index];
			}
			else if (array is StringObject)
			{
				string str = ((StringObject)array).String;
				return index < str.Length ? new StringObject(str[index].ToString()) : null;
			}
			else if (srm.EnableDirectAccess && srm.IsDirectAccessObject(array))
			{
				if (array is IList)
				{
					return ((IList)array)[index];
				}
				else
					return null;
			}
			else
			{
				return null;
			}
		}
		#endregion
	}
	#endregion

	#region Property Access
	class PropertyAccess : AccessValue
	{
		private object obj;
		public object Object
		{
			get { return obj; }
			set { obj = value; }
		}
		private string identifier;
		public string Identifier
		{
			get { return identifier; }
			set { identifier = value; }
		}
		public PropertyAccess(object obj, string identifer, ScriptRunningMachine srm)
			: base(srm)
		{
			this.obj = obj;
			this.identifier = identifer;
		}
		#region Access Members
		public override void Set(object value)
		{
			PropertyAccessHelper.SetProperty(srm, obj, identifier, value);
		}
		public override object Get()
		{
			return PropertyAccessHelper.GetProperty(srm, obj, identifier);
		}
		#endregion
	}

	sealed class PropertyAccessHelper
	{
		internal static void SetProperty(ScriptRunningMachine srm, object target, string identifier, object value)
		{
			// in DirectAccess mode and object is accessable directly
			if (srm.EnableDirectAccess && srm.IsDirectAccessObject(target))
			{
				string memberName = ScriptRunningMachine.GetNativeIdentifierName(identifier);

				// if value is anonymous function, try to attach CLR event
				if (value is FunctionValue)
				{
					if (srm.EnableCLREvent)
					{
						EventInfo ei = target.GetType().GetEvent(memberName);
						if (ei != null)
						{
							srm.AttachEvent(target, ei, value as FunctionValue);

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
							if (target is Control && ((Control)target).InvokeRequired)
							{
								((Control)target).Invoke((MethodInvoker)(() =>
								{
									pi.SetValue(target, srm.ConvertToCLRType(value, pi.PropertyType), null);
								}));
							}
							else
							{
								pi.SetValue(target, srm.ConvertToCLRType(value, pi.PropertyType), null);
							}
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
							if (value == null && srm.EnableCLREvent)
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
			else if (target is ObjectValue)
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
			else
			{
				// unknown type, ignore it
			}
		}

		internal static object GetProperty(ScriptRunningMachine srm, object target, string identifier)
		{
			if (srm.EnableDirectAccess && srm.IsDirectAccessObject(target))
			{
				string memberName = ScriptRunningMachine.GetNativeIdentifierName(identifier);

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
						else
						{
							// can not found property or field, return undefined
							return null;
						}
					}
				}
			}
			else if (target is ObjectValue)
			{
				ObjectValue objValue = (ObjectValue)target;
				object val = objValue[identifier];
				if (val is ExternalProperty)
				{
					return (((ExternalProperty)val).GetNativeValue());
				}
				else
				{
					return objValue[identifier];
				}
			}
			else
			{
				// unknown type, return undefined
				return null;
			}
		}

	}
	#endregion

	#endregion

	#region Parsers
	#region Parser Interface
	interface INodeParser
	{
		object Parse(CommonTree t, ScriptRunningMachine srm);
	}
	#endregion

	#region Import Statement
	class ImportNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < t.ChildCount; i++)
			{
				string ns = t.Children[i].ToString();

				if (i == t.ChildCount - 1)
				{
					if (ns == "*")
					{
						srm.ImportNamespace(sb.ToString());
						return null;
					}
					else
					{
						string name = sb.ToString();
						if (name.EndsWith(".")) name = name.Substring(0, name.Length - 1);

						Type type = srm.GetTypeFromAssembly(name, ns);
						if (type != null)
						{
							srm.ImportType(type);
						}
						return null;
					}
				}

				sb.Append(ns);
			}

			return null;
		}

		#endregion
	}
	#endregion
	#region Local Variable Declaration
	class DeclarationNodeParser : INodeParser
	{
		private AssignmentNodeParser assignmentParser = new AssignmentNodeParser();

		#region INodeParser Members
		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object lastValue = null;
			for (int i = 1; i < t.ChildCount; i++)
			{
				CommonTree assignmentNode = (CommonTree)t.Children[i];
				//srm.CurrentContext.DeclareVariable(assignmentNode.Children[0].ToString());				
				lastValue = assignmentParser.Parse(assignmentNode, srm);
			}

			return lastValue;
		}
		#endregion
	}
	#endregion
	#region Assignment =
	class AssignmentNodeParser : INodeParser
	{
		private static readonly ExprPlusNodeParser exprPlusNodeParser = new ExprPlusNodeParser();

		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			if (t.ChildCount == 1)
			{
				return null;
			}

			IAccess access = srm.ParseNode((CommonTree)t.Children[0]) as IAccess;
			if (access == null)
			{
				if (srm.IsInGlobalScope)
				{
					access = new PropertyAccess(srm.CurrentContext.GlobalObject, t.Children[0].Text, srm);
				}
			}

			CommonTree expr = t.ChildCount > 1 ? (CommonTree)t.Children[1] : null;

			object value = null;
			if (expr != null)
			{
				value = srm.ParseNode(expr);
			}

			if (value is IAccess) value = ((IAccess)value).Get();

			if (access != null)
			{
				access.Set(value);
			}
			else if (!srm.IsInGlobalScope)
			{
				srm.CurrentContext[t.Children[0].Text] = value;
			}

			return value;
		}
		#endregion
	}
	#endregion
	#region Expresion Operator
	abstract class ExpressionOperatorNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object left = srm.ParseNode((CommonTree)t.Children[0]);
			if (left is IAccess) left = ((IAccess)left).Get();

			object right = srm.ParseNode((CommonTree)t.Children[1]);
			if (right is IAccess) right = ((IAccess)right).Get();

			return Calc(left, right, srm);
		}

		public abstract object Calc(object left, object right, ScriptRunningMachine srm);

		#endregion
	}
	abstract class MathExpressionOperatorParser : ExpressionOperatorNodeParser
	{
		public override object Calc(object left, object right, ScriptRunningMachine srm)
		{
			try
			{
				return MathCalc(Convert.ToDouble(left), Convert.ToDouble(right));
			}
			catch
			{
				return NaNValue.Value;
			}
		}

		public abstract object MathCalc(double left, double right);
	}
	#region Plus +
	class ExprPlusNodeParser : ExpressionOperatorNodeParser
	{
		public override object Calc(object left, object right, ScriptRunningMachine srm)
		{
			if ((left is double || left is int || left is long)
				&& (right is double || right is int || right is long))
			{
				return Convert.ToDouble(left) + Convert.ToDouble(right);
			}
			else if (left is string || right is string
				|| left is StringObject || right is StringObject)
			{
				return new StringObject(string.Empty + left + right);
			}
			else if (left.GetType() == typeof(ObjectValue) && right.GetType() == typeof(ObjectValue))
			{
				ObjectValue obj = srm.CreateNewObject(((ObjectValue)left).Name);
				srm.CombileObject(obj, ((ObjectValue)left));
				srm.CombileObject(obj, ((ObjectValue)right));
				return obj;
			}
			else
			{
				try
				{
					return Convert.ToDouble(left) + Convert.ToDouble(right);
				}
				catch (Exception)
				{
					return string.Empty + left + right;
				}
			}
		}
	}
	#endregion
	#region Minus -
	class ExprMinusNodeParser : MathExpressionOperatorParser
	{
		public override object MathCalc(double left, double right)
		{
			return left - right;
		}
	}
	#endregion
	#region Mul *
	class ExprMultiNodeParser : MathExpressionOperatorParser
	{
		public override object MathCalc(double left, double right)
		{
			return left * right;
		}
	}
	#endregion
	#region Div /
	class ExprDivNodeParser : MathExpressionOperatorParser
	{
		public override object MathCalc(double left, double right)
		{
			return left / right;
		}
	}
	#endregion
	#region Mod %
	class ExprModNodeParser : MathExpressionOperatorParser
	{
		#region INodeParser Members

		public override object MathCalc(double left, double right)
		{
			return (left % right);
		}

		#endregion
	}
	#endregion
	#region And &
	class ExprAndNodeParser : ExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override object Calc(object left, object right, ScriptRunningMachine srm)
		{
			if (left is long && right is long)
			{
				return ((long)left & (long)right);
			}
			else
			{
				return NaNValue.Value;
			}
		}

		#endregion
	}
	#endregion
	#region Or |
	class ExprOrNodeParser : ExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override object Calc(object left, object right, ScriptRunningMachine srm)
		{
			if (left is long && right is long)
			{
				return ((long)left | (long)right);
			}
			else
			{
				return NaNValue.Value;
			}
		}

		#endregion
	}
	#endregion
	#region Xor ^
	class ExprXorNodeParser : ExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override object Calc(object left, object right, ScriptRunningMachine srm)
		{
			if (left is long && right is long)
			{
				return ((long)left ^ (long)right);
			}
			else
			{
				return NaNValue.Value;
			}
		}

		#endregion
	}
	#endregion
	#region Left Shift <<
	class ExprLeftShiftNodeParser : ExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override object Calc(object left, object right, ScriptRunningMachine srm)
		{
			try
			{
				int l = Convert.ToInt32(left);
				int r = Convert.ToInt32(right);

				return l << r;
			}
			catch
			{
				return 0;
			}
		}

		#endregion
	}
	#endregion
	#region Right Shift >>
	class ExprRightShiftNodeParser : ExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override object Calc(object left, object right, ScriptRunningMachine srm)
		{
			try
			{
				int l = Convert.ToInt32(left);
				int r = Convert.ToInt32(right);

				return l >> r;
			}
			catch
			{
				return 0;
			}
		}

		#endregion
	}
	#endregion
	#endregion
	#region Unary for Primary Expression
	class ExprUnaryNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			string oprt = t.Children[0].ToString();
			object value = srm.ParseNode((CommonTree)t.Children[1]);

			if (value == null) return null;

			switch (oprt)
			{
				default:
				case "+":
					return value;

				case "-":
					if (value is long)
						return -((long)value);
					else if (value is double)
						return -((double)value);
					else
						return null;

				case "!":
					if (!(value is bool))
					{
						return null;
					}
					return !((bool)value);

				case "~":
					return (~(Convert.ToInt64(value)));
			}
		}

		#endregion
	}
	#endregion
	#region Post Increment i++
	class ExprPostIncrementNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			CommonTree target = (CommonTree)t.Children[0];
			IAccess access = srm.ParseNode((CommonTree)t.Children[0]) as IAccess;
			if (access == null)
			{
				throw new AWDLRuntimeException("only property, indexer, and variable can be used as increment or decrement statement.");
			}

			object oldValue = access.Get();
			if (oldValue == null)
			{
				oldValue = 0;
			}

			if (!(oldValue is double || oldValue is int || oldValue is long))
			{
				throw new AWDLRuntimeException("only interger can be used as increment or decrement statement.");
			}

			double value = Convert.ToDouble(oldValue);
			double returnValue = value;
			access.Set((value + (t.Children[1].Type == ReoScriptLexer.INCREMENT ? 1 : -1)));
			return returnValue;
		}

		#endregion
	}
	#endregion
	#region Pre Increment ++i
	class ExprPreIncrementNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			CommonTree target = (CommonTree)t.Children[0];
			IAccess access = srm.ParseNode((CommonTree)t.Children[0]) as IAccess;
			if (access == null)
			{
				throw new AWDLRuntimeException("only property, indexer, and variable can be used as increment or decrement statement.");
			}
			object oldValue = access.Get();
			if (oldValue == null)
			{
				oldValue = 0;
			}

			if (!(oldValue is double || oldValue is int || oldValue is long))
			{
				throw new AWDLRuntimeException("only interger can be used as increment or decrement statement.");
			}

			double value = Convert.ToDouble(oldValue);

			object v = (value + (t.Children[1].Type == ReoScriptLexer.INCREMENT ? 1 : -1));
			access.Set(v);
			return v;
		}

		#endregion
	}
	#endregion
	#region Condition ? :
	class ExprConditionNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object value = srm.ParseNode((CommonTree)t.Children[0]);
			if (!(value is bool))
			{
				throw new AWDLRuntimeException("only boolean expression can be used for conditional expression.");
			}
			bool condition = (bool)value;
			return condition ? srm.ParseNode((CommonTree)t.Children[1]) : srm.ParseNode((CommonTree)t.Children[2]);
		}

		#endregion
	}
	#endregion
	#region Relation Expression Operator
	abstract class RelationExpressionOperatorNodeParser : ExpressionOperatorNodeParser
	{
		#region INodeParser Members
		public override object Calc(object left, object right, ScriptRunningMachine srm)
		{
			return Compare(left, right);
		}

		public abstract bool Compare(object left, object right);
		#endregion
	}
	#region Equals ==
	class ExprEqualsNodeParser : RelationExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override bool Compare(object left, object right)
		{
			if (left == null && right == null) return true;
			if (left == null || right == null) return false;

			if (left is StringObject)
			{
				return ((StringObject)left).Equals(right);
			}
			else if (right is StringObject)
			{
				return ((StringObject)right).Equals(left);
			}
			else
			{
				return left.Equals(right);
			}
		}

		#endregion
	}
	#endregion
	#region Not Equals !=
	class ExprNotEqualsNodeParser : RelationExpressionOperatorNodeParser
	{
		private static readonly ExprEqualsNodeParser equalsParser = new ExprEqualsNodeParser();
		#region INodeParser Members

		public override bool Compare(object left, object right)
		{
			return !equalsParser.Compare(left, right);
		}

		#endregion
	}
	#endregion
	#region Greater Than >
	class ExprGreaterThanNodeParser : RelationExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override bool Compare(object left, object right)
		{
			if ((left is int || left is long || left is double)
			&& (right is int || right is long || right is double))
			{
				return Convert.ToDouble(left) > Convert.ToDouble(right);
			}
			else
			{
				return false;
			}
		}

		#endregion
	}
	#endregion
	#region Greater Or Equals >=
	class ExprGreaterOrEqualsNodeParser : RelationExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override bool Compare(object left, object right)
		{
			if ((left is int || left is long || left is double)
			&& (right is int || right is long || right is double))
			{
				return Convert.ToDouble(left) >= Convert.ToDouble(right);
			}
			else
			{
				return false;
			}
		}

		#endregion
	}
	#endregion
	#region Less Than <
	class ExprLessThanNodeParser : RelationExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override bool Compare(object left, object right)
		{
			if ((left is int || left is long || left is double)
				&& (right is int || right is long || right is double))
			{
				return Convert.ToDouble(left) < Convert.ToDouble(right);
			}
			else
			{
				return false;
			}
		}

		#endregion
	}
	#endregion
	#region Less Or Equals <=
	class ExprLessOrEqualsNodeParser : RelationExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override bool Compare(object left, object right)
		{
			if ((left is int || left is long || left is double)
				&& (right is int || right is long || right is double))
			{
				return Convert.ToDouble(left) <= Convert.ToDouble(right);
			}
			else
			{
				return false;
			}
		}

		#endregion
	}
	#endregion
	#endregion
	#region Boolean Expression Operator
	abstract class BooleanExpressionOperatorNodeParser : INodeParser
	{
		#region INodeParser Members
		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object left = srm.ParseNode((CommonTree)t.Children[0]);
			//if (left == null) throw new InvalidExpressionElementException("invalid boolean expression element: " + t.Children[0].ToString());

			object right = srm.ParseNode((CommonTree)t.Children[1]);
			//if (right == null) throw new InvalidExpressionElementException("invalid boolean expression element: " + t.Children[1].ToString());

			if (!(left is bool) || !(right is bool))
				return false;
			else
				return Calc((bool)left, (bool)right);
		}
		#endregion

		public abstract bool Calc(bool left, bool right);
	}
	#region Boolean And &&
	class BooleanAndNodeParser : BooleanExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override bool Calc(bool left, bool right)
		{
			return left && right;
		}

		#endregion
	}
	#endregion
	#region Boolean Or ||
	class BooleanOrNodeParser : BooleanExpressionOperatorNodeParser
	{
		#region INodeParser Members

		public override bool Calc(bool left, bool right)
		{
			return left || right;
		}

		#endregion
	}
	#endregion
	#endregion
	#region If Else
	class IfStatementNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object value = srm.ParseNode((CommonTree)t.Children[0]);
			if (!(value is bool))
			{
				return false;
				//throw new AWDLRuntimeException("only boolean expression can be used as test condition.");
			}
			bool condition = (bool)value;
			if (condition)
			{
				return srm.ParseNode((CommonTree)t.Children[1]);
			}
			else if (t.ChildCount == 3)
			{
				return srm.ParseNode((CommonTree)t.Children[2]);
			}
			else
				return null;
		}

		#endregion
	}
	#endregion
	#region Switch Case
	class SwitchCaseStatementNodeParser : INodeParser
	{
		private ExprEqualsNodeParser equalsParser = new ExprEqualsNodeParser();

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			if (t.ChildCount == 0) return null;

			object source = srm.ParseNode(t.Children[0] as CommonTree);
			while (source is IAccess) source = ((IAccess)source).Get();

			if (source == null) return null;

			int defaultCaseLine = 0;
			bool doParse = false;

			int i = 1;

		doDefault:
			while (i < t.ChildCount)
			{
				CommonTree caseTree = t.Children[i] as CommonTree;

				if (caseTree.Type == ReoScriptLexer.BREAK)
				{
					if (doParse) return null;
				}
				else if (caseTree.Type == ReoScriptLexer.SWITCH_CASE_ELSE)
				{
					defaultCaseLine = i;
				}
				else if (caseTree.Type == ReoScriptLexer.SWITCH_CASE)
				{
					if (caseTree.ChildCount > 0)
					{
						object target = srm.ParseNode(caseTree.Children[0] as CommonTree);
						if (target is IAccess) target = ((IAccess)target).Get();

						if ((bool)equalsParser.Calc(source, target, srm))
						{
							doParse = true;
						}
					}
				}
				else if (doParse)
				{
					srm.ParseNode(caseTree);
				}

				i++;
			}

			if (defaultCaseLine > 0)
			{
				i = defaultCaseLine;
				doParse = true;
				goto doDefault;
			}

			return null;
		}
	}
	#endregion
	#region For
	class ForStatementNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			CommonTree forInit = (CommonTree)t.Children[0];
			srm.ParseChildNodes(forInit);

			CommonTree condition = (CommonTree)t.Children[1];
			CommonTree forIterator = (CommonTree)t.Children[2];
			CommonTree body = (CommonTree)t.Children[3];

			while (true)
			{
				object conditionValue = srm.ParseNode(condition) as object;
				if (conditionValue != null)
				{
					bool? booleanValue = conditionValue as bool?;

					if (booleanValue == null)
					{
						throw new AWDLRuntimeException("only boolean expression can be used as test condition.");
					}
					else if (!((bool)booleanValue))
					{
						return null;
					}
				}

				object result = srm.ParseNode(body);
				if (result is BreakNode)
				{
					return null;
				}

				srm.ParseNode(forIterator);
			}
		}

		#endregion
	}
	#endregion
	class ForEachStatementNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			string varName = t.Children[0].ToString();

			// retrieve target object
			object nativeObj = srm.ParseNode(t.Children[1] as CommonTree);
			if (nativeObj is IAccess) nativeObj = ((IAccess)nativeObj).Get();

			if (nativeObj is IEnumerable)
			{
				IEnumerator iterator = ((IEnumerable)nativeObj).GetEnumerator();
				while (iterator.MoveNext())
				{
					// prepare key
					srm[varName] = iterator.Current;

					// prepare iterator
					CommonTree body = t.Children[2] as CommonTree;

					// call iterator and terminal loop if break
					object result = srm.ParseNode(body);
					if (result is BreakNode)
					{
						return null;
					}
				}
			}

			return null;
		}

		#endregion
	}

	#region Break
	class BreakNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			return new BreakNode();
		}

		#endregion
	}
	#endregion
	#region Continue
	class ContinueNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			return new ContinueNode();
		}

		#endregion
	}
	#endregion
	#region Return
	class ReturnNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object v = srm.ParseNode((CommonTree)t.Children[0]);
			if (v is IAccess) v = ((IAccess)v).Get();
			return new ReturnNode(v);
		}

		#endregion
	}
	#endregion
	#region Function Define
	class FunctionDefineNodeParser : INodeParser
	{
		private AssignmentNodeParser assignParser = new AssignmentNodeParser();

		#region INodeParser Members
		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			CommonTree paramsTree = t.Children[1] as CommonTree;
			string[] identifiers = new string[paramsTree.ChildCount];

			for (int i = 0; i < identifiers.Length; i++)
			{
				identifiers[i] = paramsTree.Children[i].ToString();
			}

			string funName = t.Children[0].Text;

			FunctionValue fun = new FunctionValue()
			{
				Name = funName,
				Args = identifiers,
				Body = (CommonTree)t.Children[2],
			};

			if (srm.IsInGlobalScope)
			{
				srm.CurrentContext.GlobalObject[fun.Name] = fun;
			}
			else
			{
				srm.CurrentContext.GetCurrentCallScope()[fun.Name] = fun;
			}

			return null;
		}
		#endregion
	}
	#endregion
	#region AnonymousFunction Define
	class AnonymousFunctionNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			FunctionValue fun = new FunctionValue();

			string[] identifiers = new string[((CommonTree)t.Children[0]).ChildCount];
			for (int i = 0; i < identifiers.Length; i++)
				identifiers[i] = ((CommonTree)t.Children[0]).Children[i].ToString();
			fun.Args = identifiers;
			fun.Body = (CommonTree)t.Children[1];

			return fun;
		}

		#endregion
	}

	#endregion
	#region Function Call
	class FunctionCallNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object funObj = null;
			object ownerObj = null;

			// local-function call
			if (t.Children[0].Type == ReoScriptLexer.IDENTIFIER)
			{
				string funName = t.Children[0].ToString();
				funObj = srm.CurrentContext[funName];
			}
			else
			{
				// other objects call
				funObj = srm.ParseNode(t.Children[0] as CommonTree);

				if (funObj is PropertyAccess)
				{
					ownerObj = ((PropertyAccess)funObj).Object;
					string methodName = ((PropertyAccess)funObj).Identifier;

					if (srm.IsDirectAccessObject(ownerObj))
					{
						if (!srm.EnableDirectAccess)
						{
							// owner object is not ReoScript object and DirectAccess is disabled.
							// there is nothing can do so just return undefined.
							return null;
						}
						else
						{
							if (srm.EnableDirectAccess && srm.IsDirectAccessObject(ownerObj))
							{
								object[] args = srm.GetParameterList(
										(t.ChildCount == 0 ? null : t.Children[1] as CommonTree));

								MethodInfo mi = ScriptRunningMachine.FindCLRMethodAmbiguous(ownerObj,
									ScriptRunningMachine.GetNativeIdentifierName(methodName), args);

								if (mi != null)
								{
									ParameterInfo[] paramTypeList = mi.GetParameters();

									//if (paramTypeList.Length != args.Length)
									//{
									//  if (srm.IgnoreCLRExceptions)
									//  {
									//    return null;
									//  }
									//  else
									//  {
									//    throw new AWDLRuntimeException("Direct access to CLR method with not matched argument list: " + methodName);
									//  }
									//}

									try
									{
										object[] convertedArgs = new object[args.Length];
										for (int i = 0; i < convertedArgs.Length; i++)
										{
											convertedArgs[i] = srm.ConvertToCLRType(args[i], paramTypeList[i].ParameterType);
										}
										return mi.Invoke(ownerObj, convertedArgs);
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
							}
						}
					}

					funObj = ((IAccess)funObj).Get();

					if (funObj == null)
					{
						throw new AWDLRuntimeException(string.Format("{0} has no method '{1}'", ownerObj, methodName));
					}
				}
				else
				{
					while (funObj is IAccess) funObj = ((IAccess)funObj).Get();
				}
			}

			if (funObj == null)
			{
				throw new AWDLRuntimeException("Function is not defined: " + t.Children[0].ToString());
			}

			if (ownerObj == null) ownerObj = srm.CurrentContext.ThisObject;

			if (!(funObj is AbstractFunctionValue))
			{
				throw new AWDLRuntimeException("Object is not a function: " + Convert.ToString(funObj));
			}

			return srm.InvokeFunction(ownerObj, ((AbstractFunctionValue)funObj), t.ChildCount == 0 ? null : t.Children[1] as CommonTree);
		}

		#endregion
	}
	#endregion
	#region Create
	class CreateObjectNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			// combiled construct
			if (t.ChildCount > 0 && t.Children[0].Type == ReoScriptLexer.COMBINE_OBJECT)
			{
				CommonTree combileTree = ((CommonTree)t.Children[0]);
				ObjectValue combileObj = srm.ParseNode(combileTree.Children[1] as CommonTree) as ObjectValue;

				object createdObj = Parse(combileTree.Children[0] as CommonTree, srm);
				srm.CombileObject(createdObj, combileObj);
				return createdObj;
			}

			CommonTree ct = t.Children[0] as CommonTree;

			while (ct != null && ct.Children != null)
			{
				if (ct.Type == ReoScriptLexer.FUNCTION_CALL)
				{
					t = ct;
					break;
				}

				ct = ct.Children[0] as CommonTree;
			}

			if (t == null) throw new AWDLRuntimeException("unexpected end in new operation.");

			object value = srm.ParseNode(t.Children[0] as CommonTree);

			string constructorName = t.Children[0].Type == ReoScriptLexer.IDENTIFIER ? t.Children[0].Text : "undefined";

			if (value is IAccess) value = ((IAccess)value).Get();

			if (value == null && srm.EnableDirectAccess)
			{
				Type type = srm.GetImportedTypeFromNamespaces(constructorName);
				if (type != null)
				{
					value = new TypedNativeFunctionValue(type, type.Name, null);
				}
			}

			if (value == null)
			{
				throw new AWDLRuntimeException("Constructor function not found: " + constructorName);
			}

			if (!(value is AbstractFunctionValue))
			{
				throw new AWDLRuntimeException("Constructor is not a function type: " + constructorName);
			}
			else
			{
				AbstractFunctionValue funObj = (AbstractFunctionValue)value;
				return srm.CreateNewObject(funObj, t);
			}
		}

		#endregion
	}
	#endregion
	#region ArrayAccess
	class ArrayAccessNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object value = srm.ParseNode((CommonTree)t.Children[0]);
			if (value is IAccess) value = ((IAccess)value).Get();

			if (value == null)
			{
				throw new AWDLRuntimeException("Attempt to access an array that is null or undefined.");
			}

			object indexValue = srm.ParseNode((CommonTree)t.Children[1]);
			while (indexValue is IAccess) indexValue = ((IAccess)indexValue).Get();
			int index = ScriptRunningMachine.GetIntValue(indexValue);

			return new ArrayAccess(value, index, srm);
		}

		#endregion
	}
	#endregion
	#region PropertyAccess
	class PropertyAccessNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm)
		{
			object value = null;

			value = srm.ParseNode((CommonTree)t.Children[0]);
			while (value is IAccess) value = ((IAccess)value).Get();

			if (value == null) throw new AWDLRuntimeException("Attempt to access property from null or undefined object.");

			if (!(value is ObjectValue))
			{
				if (value is ISyntaxTreeReturn)
				{
					throw new AWDLRuntimeException(string.Format("Attempt to access an object '{0}' that is not Object type.", value.ToString()));
				}
				else if (!srm.EnableDirectAccess)
				{
					throw new AWDLRuntimeException(string.Format("Attempt to access an object '{0}' that is not Object type. Make sure that object type is Object. Or to access a .Net object, set WorkMode to enable DirectAccess.", value.ToString()));
				}
			}

			return new PropertyAccess(value, t.Children[1].ToString(), srm);
		}

		#endregion
	}
	#endregion
	#endregion

	#region Parser Adapter
	#region Define Interface
	interface IParserAdapter
	{
		INodeParser MatchParser(CommonTree t);
	}
	#endregion

	#region AWDLDefaultParserAdapter
	class AWDLLogicSyntaxParserAdapter : IParserAdapter
	{
		private static readonly INodeParser[] definedParser = new INodeParser[200];

		static AWDLLogicSyntaxParserAdapter()
		{
			#region Predefined Parsers
			definedParser[ReoScriptLexer.IMPORT] = new ImportNodeParser();
			definedParser[ReoScriptLexer.DECLARATION] = new DeclarationNodeParser();
			definedParser[ReoScriptLexer.ASSIGNMENT] = new AssignmentNodeParser();
			definedParser[ReoScriptLexer.IF_STATEMENT] = new IfStatementNodeParser();
			definedParser[ReoScriptLexer.FOR_STATEMENT] = new ForStatementNodeParser();
			definedParser[ReoScriptLexer.FOREACH_STATEMENT] = new ForEachStatementNodeParser();
			definedParser[ReoScriptLexer.SWITCH] = new SwitchCaseStatementNodeParser();
			definedParser[ReoScriptLexer.FUNCTION_CALL] = new FunctionCallNodeParser();
			definedParser[ReoScriptLexer.FUNCTION_DEFINE] = new FunctionDefineNodeParser();
			definedParser[ReoScriptLexer.ANONYMOUS_FUNCTION_DEFINE] = new AnonymousFunctionNodeParser();
			definedParser[ReoScriptLexer.BREAK] = new BreakNodeParser();
			definedParser[ReoScriptLexer.CONTINUE] = new ContinueNodeParser();
			definedParser[ReoScriptLexer.RETURN] = new ReturnNodeParser();
			definedParser[ReoScriptLexer.CREATE] = new CreateObjectNodeParser();
			definedParser[ReoScriptLexer.ARRAY_ACCESS] = new ArrayAccessNodeParser();
			definedParser[ReoScriptLexer.PROPERTY_ACCESS] = new PropertyAccessNodeParser();
			#endregion

			#region Operators
			definedParser[ReoScriptLexer.PLUS] = new ExprPlusNodeParser();
			definedParser[ReoScriptLexer.MINUS] = new ExprMinusNodeParser();
			definedParser[ReoScriptLexer.MUL] = new ExprMultiNodeParser();
			definedParser[ReoScriptLexer.DIV] = new ExprDivNodeParser();
			definedParser[ReoScriptLexer.MOD] = new ExprModNodeParser();
			definedParser[ReoScriptLexer.AND] = new ExprAndNodeParser();
			definedParser[ReoScriptLexer.OR] = new ExprOrNodeParser();
			definedParser[ReoScriptLexer.XOR] = new ExprXorNodeParser();
			definedParser[ReoScriptLexer.LSHIFT] = new ExprLeftShiftNodeParser();
			definedParser[ReoScriptLexer.RSHIFT] = new ExprRightShiftNodeParser();
			#endregion

			#region Unary Operators
			definedParser[ReoScriptLexer.PRE_UNARY] = new ExprUnaryNodeParser();
			definedParser[ReoScriptLexer.PRE_UNARY_STEP] = new ExprPreIncrementNodeParser();
			definedParser[ReoScriptLexer.POST_UNARY_STEP] = new ExprPostIncrementNodeParser();
			#endregion

			#region Relation Operators
			definedParser[ReoScriptLexer.CONDITION] = new ExprConditionNodeParser();
			definedParser[ReoScriptLexer.EQUALS] = new ExprEqualsNodeParser();
			definedParser[ReoScriptLexer.NOT_EQUALS] = new ExprNotEqualsNodeParser();
			definedParser[ReoScriptLexer.GREAT_THAN] = new ExprGreaterThanNodeParser();
			definedParser[ReoScriptLexer.GREAT_EQUALS] = new ExprGreaterOrEqualsNodeParser();
			definedParser[ReoScriptLexer.LESS_THAN] = new ExprLessThanNodeParser();
			definedParser[ReoScriptLexer.LESS_EQUALS] = new ExprLessOrEqualsNodeParser();
			#endregion

			#region Boolean Operations
			definedParser[ReoScriptLexer.LOGICAL_AND] = new BooleanAndNodeParser();
			definedParser[ReoScriptLexer.LOGICAL_OR] = new BooleanOrNodeParser();
			#endregion
		}

		#region IParserAdapter Members

		public virtual INodeParser MatchParser(CommonTree t)
		{
			return definedParser[t.Type];
		}

		#endregion
	}
	#endregion

	#endregion

	#region ScriptContext
	internal class ScriptContext
	{
		public ScriptContext()
		{
		}

		public ObjectValue GlobalObject { get; set; }

		public object ThisObject
		{
			get
			{
				return callStack.Count == 0 ? null : callStack.Peek().ThisObject;
			}
			set
			{
				if (callStack.Count() > 0) callStack.Peek().ThisObject = value;
			}
		}

		public ObjectValue WithObject { get; set; }

		public object this[string identifier]
		{
			get
			{
				foreach (CallScope scope in callStack)
				{
					if (scope.Variables.ContainsKey(identifier))
					{
						return scope.Variables[identifier];
					}
				}

				return GlobalObject[identifier];
			}
			set
			{
				GetCurrentCallScope()[identifier] = value;
			}
		}

		private readonly Stack<CallScope> callStack = new Stack<CallScope>();

		public CallScope GetCurrentCallScope()
		{
			return callStack.Count > 0 ? callStack.Peek() : null;
		}

		public CallScope GetVariableScope(string identifier)
		{
			foreach (CallScope scope in callStack)
			{
				if (scope.Variables.ContainsKey(identifier))
				{
					return scope;
				}
			}
			return null;
		}

		public void PushCallStack(CallScope scope)
		{
			callStack.Push(scope);
		}

		public void PopVariableStack()
		{
			if (callStack.Count > 0) callStack.Pop();
		}

		private List<ScriptRunningMachine.EventHandlerInfo> registeredEventHandlers = new List<ScriptRunningMachine.EventHandlerInfo>();

		internal List<ScriptRunningMachine.EventHandlerInfo> RegisteredEventHandlers
		{
			get { return registeredEventHandlers; }
			set { registeredEventHandlers = value; }
		}

		private Dictionary<FunctionValue, ObjectValue> prototypes = new Dictionary<FunctionValue, ObjectValue>();

		internal Dictionary<FunctionValue, ObjectValue> Prototypes
		{
			get { return prototypes; }
			set { prototypes = value; }
		}

		private List<Type> importedTypes = new List<Type>();

		internal List<Type> ImportedTypes
		{
			get { return importedTypes; }
			set { importedTypes = value; }
		}

		private List<string> importedNamespace = new List<string>();

		internal List<string> ImportedNamespace
		{
			get { return importedNamespace; }
			set { importedNamespace = value; }
		}
	}

	internal class CallScope
	{
		public object ThisObject { get; set; }
		public FunctionValue CurrentFunction { get; set; }

		public CallScope(object thisObject, FunctionValue funObject)
		{
			this.ThisObject = thisObject;
			this.CurrentFunction = funObject;
		}

		private Dictionary<string, object> variables = new Dictionary<string, object>();

		public Dictionary<string, object> Variables
		{
			get { return variables; }
		}

		public object this[string identifier]
		{
			get
			{
				return variables.ContainsKey(identifier) ? variables[identifier] : null;
			}
			set
			{
				variables[identifier] = value;
			}
		}
	}
	#endregion

	#region ScriptRunningMachine
	/// <summary>
	/// A virtual machine to execute ReoScript language.
	/// 
	/// Property value of the global object will not be removed after script is 
	/// finished running, use multi-instance to solve this or invoke ResetContext.
	/// </summary>
	public class ScriptRunningMachine
	{
		internal ScriptContext CurrentContext { get; set; }

		/// <summary>
		/// Set value as a property to the global object. Value name specified by
		/// identifier. After this, the value can be used in script like a normal 
		/// variable.
		/// </summary>
		/// <param name="identifier">name to variable</param>
		/// <param name="obj">value of variable</param>
		public void SetGlobalVariable(string identifier, object obj)
		{
			CurrentContext.GlobalObject[identifier] = obj;
		}

		/// <summary>
		/// Get a property value from global object.
		/// 
		/// ### changed to internal because CalcExpression method can do samething
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		internal object GetGlobalVariable(string identifier)
		{
			return CurrentContext.GlobalObject[identifier];
		}

		/// <summary>
		/// Delete a specified global value.
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public bool DeleteGlobalVariable(string identifier)
		{
			return CurrentContext.GlobalObject.DeleteMember(identifier);
		}

		/// <summary>
		/// Set or get global variables
		/// </summary>
		/// <param name="identifier">identifier to variable name</param>
		/// <returns>object in global object</returns>
		public object this[string identifier]
		{
			get
			{
				return CurrentContext.GlobalObject[identifier];
			}
			set
			{
				CurrentContext.GlobalObject[identifier] = value;
			}
		}

		private static readonly FunctionValue entryFunction = new FunctionValue()
		{
			Name = "__entry__",
		};

		internal static readonly string UNDEFINED = "undefined";

		public ScriptRunningMachine()
		{
			ResetContext();
		}

		internal bool IsForceStop { get; set; }

		/// <summary>
		/// Indicate whether current machine is running. Return true if setTimeout and setInterval 
		/// makes script be executed constantly, in this time, call ForceStop may interrupt the execution.
		/// This property is readonly.
		/// </summary>
		public bool IsRunning
		{
			get
			{
				return timeoutList.Count > 0;
			}
		}

		/// <summary>
		/// Force interrupt current execution.
		/// </summary>
		public void ForceStop()
		{
			IsForceStop = true;

			//lock (asyncCallerList)
			//{
			//  asyncCallerList.Clear();
			//}
			lock (timeoutList)
			{
				foreach (BackgroundWorker bw in timeoutList)
				{
					try
					{
						if (bw != null) bw.CancelAsync();
					}
					catch { }
				}
			}
		}

		internal bool IsInGlobalScope
		{
			get
			{
				return CurrentContext.GetCurrentCallScope().CurrentFunction == entryFunction;
			}
		}

		/// <summary>
		/// Reset current context to clear all variables and restart running machine.
		/// </summary>
		public void ResetContext()
		{
			DetachAllEvents();

			ScriptContext sc = new ScriptContext();
			sc.GlobalObject = new WorldValue();
			sc.PushCallStack(new CallScope(sc.GlobalObject, entryFunction));
			CurrentContext = sc;
			LoadCoreLibraries(sc);
		}

		~ScriptRunningMachine()
		{
			DetachAllEvents();

			try
			{
				if(asyncCallThread!=null) asyncCallThread.Abort();
			}
			catch { }
		}

		internal void LoadCoreLibraries(ScriptContext sc)
		{
			using (MemoryStream ms = new MemoryStream(Resources.lib_core))
			{
				Load(ms);
			}
		}

		#region Object Management

		/// <summary>
		/// Create a new object instance 
		/// </summary>
		/// <returns>object is created</returns>
		public ObjectValue CreateNewObject()
		{
			return CreateNewObject("Object");
		}

		/// <summary>
		/// Create a new object instance
		/// </summary>
		/// <param name="name">name to an object</param>
		/// <returns>object is created</returns>
		public ObjectValue CreateNewObject(string name)
		{
			ObjectValue obj = CreateNewObject(WorldValue.ObjectFunction) as ObjectValue;
			obj["name"] = name;
			return obj;
		}

		internal object CreateNewObject(AbstractFunctionValue funObject)
		{
			return CreateNewObject(funObject, true);
		}

		internal object CreateNewObject(AbstractFunctionValue funObject, bool invoke)
		{
			return CreateNewObject(funObject, invoke, null);
		}

		internal object CreateNewObject(AbstractFunctionValue funObject, CommonTree createTree)
		{
			return CreateNewObject(funObject, true, createTree);
		}

		internal object CreateNewObject(AbstractFunctionValue funObject, bool invoke, CommonTree createTree)
		{
			object obj = null;

			if (funObject is NativeFunctionValue)
			{
				obj = ((NativeFunctionValue)funObject).CreateObject(this,
					(createTree == null || createTree.ChildCount < 2) ? null : createTree.Children[1] as CommonTree);
			}

			if (obj == null) obj = new ObjectValue();

			if (obj is ObjectValue)
			{
				ObjectValue objValue = obj as ObjectValue;

				objValue["constructor"] = funObject;
				objValue["name"] = new StringObject(funObject.Name);
			}

			if (invoke)
			{
				InvokeFunction(obj, funObject, (createTree == null || createTree.ChildCount < 2) ? null : createTree.Children[1] as CommonTree);
			}

			return obj;
		}


		/// <summary>
		/// Import a .Net type into script context. This method will creates a constructor function
		/// which named by type's name and stored as property in global object. Note that if there 
		/// is an object named type's name does exists in global object then it will be overwritten.
		/// </summary>
		/// <param name="type">type to be added into script context</param>
		public void ImportType(Type type)
		{
			ScriptContext ctx = CurrentContext;

			if (ctx.ImportedTypes.Contains(type))
			{
				ctx.ImportedTypes.Remove(type);
			}

			ctx.ImportedTypes.Add(type);

			SetGlobalVariable(type.Name, new TypedNativeFunctionValue(type, type.Name, null));
		}

		/// <summary>
		/// Import a namespace into script context
		/// </summary>
		/// <param name="name">namespace to be registered into script context</param>
		public void ImportNamespace(string name)
		{
			if (name.EndsWith("*")) name = name.Substring(0, name.Length - 1);
			if (name.EndsWith(".")) name = name.Substring(0, name.Length - 1);

			if (!CurrentContext.ImportedNamespace.Contains(name))
			{
				CurrentContext.ImportedNamespace.Add(name);
			}
		}

		internal Type GetImportedTypeFromNamespaces(string typeName)
		{
			Type type = null;

			foreach (string ns in CurrentContext.ImportedNamespace)
			{
				type = GetTypeFromAssembly(ns, typeName);
				if (type != null) return type;
			}

			return type;
		}

		internal Type GetTypeFromAssembly(string ns, string typeName)
		{
			Type type = null;

			// search assembly which's name starting with specified namespace
			Assembly ass = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(q => q.FullName.StartsWith(ns));

			if (ass != null)
			{
				type = ass.GetType(ns + "." + typeName);
				if (type != null)
				{
					ImportType(type);
					return type;
				}
			}

			return type;
		}

		internal void CombileObject(object target, ObjectValue source)
		{
			foreach (string key in source)
			{
				PropertyAccessHelper.SetProperty(this, target, key, source[key]);
			}
		}

		#endregion

		#region CLR Event

		internal void AttachEvent(object obj, EventInfo ei, FunctionValue functionValue)
		{
			// remove last attached event to sample object
			DetachEvent(obj, ei);

			EventHandlerInfo ehi = new EventHandlerInfo(this, obj, ei, null, functionValue);
			Action<object> doEvent = (e) => { InvokeFunction(obj, functionValue, new object[] { e }); };

			Delegate d = null;
			if (ei.EventHandlerType == typeof(EventHandler))
			{
				d = new EventHandler((s, e) => doEvent(e));
			}
			else if (ei.EventHandlerType == typeof(MouseEventHandler))
			{
				d = new MouseEventHandler((s, e) => doEvent(e));
			}
			else if (ei.EventHandlerType == typeof(KeyEventHandler))
			{
				d = new KeyEventHandler((s, e) => doEvent(e));
			}
			else if (ei.EventHandlerType == typeof(PaintEventHandler))
			{
				d = new PaintEventHandler((s, e) => doEvent(e));
			}

			ehi.ActionMethod = d;
			ei.AddEventHandler(obj, d);

			CurrentContext.RegisteredEventHandlers.Add(ehi);
			return;

			// Get an EventInfo representing the Click event, and get the 
			// type of delegate that handles the event. 
			//
			EventInfo evClick = ei;
			Type tDelegate = evClick.EventHandlerType;

			// If you already have a method with the correct signature, 
			// you can simply get a MethodInfo for it.  
			//
			//MethodInfo miHandler =
			//    typeof(Example).GetMethod("LuckyHandler",
			//        BindingFlags.NonPublic | BindingFlags.Instance);

			// Create an instance of the delegate. Using the overloads 
			// of CreateDelegate that take MethodInfo is recommended. 
			//
			//Delegate d = Delegate.CreateDelegate(tDelegate, this, miHandler);

			// Get the "add" accessor of the event and invoke it late-
			// bound, passing in the delegate instance. This is equivalent 
			// to using the += operator in C#, or AddHandler in Visual 
			// Basic. The instance on which the "add" accessor is invoked
			// is the form; the arguments must be passed as an array. 
			//
			MethodInfo addHandler = evClick.GetAddMethod();
			//Object[] addHandlerArgs = { d };
			//addHandler.Invoke(exFormAsObj, addandlerArgs);
			//---------------------------------------------

			// Event handler methods can also be generated at run time, 
			// using lightweight dynamic methods and Reflection.Emit.  
			// To construct an event handler, you need the return type 
			// and parameter types of the delegate. These can be obtained 
			// by examining the delegate's Invoke method.  
			// 
			// It is not necessary to name dynamic methods, so the empty  
			// string can be used. The last argument associates the  
			// dynamic method with the current type, giving the delegate 
			// access to all the public and private members of Example, 
			// as if it were an instance method. 
			//
			Type returnType = GetDelegateReturnType(tDelegate);
			if (returnType != typeof(void))
				throw new ApplicationException("Delegate has a return type.");

			DynamicMethod handler =
					new DynamicMethod(string.Empty,
														null,
														GetDelegateParameterTypes(tDelegate),
														typeof(ScriptRunningMachine));

			// Generate a method body. This method loads a string, calls  
			// the Show method overload that takes a string, pops the  
			// return value off the stack (because the handler has no 
			// return type), and returns. 
			//
			ILGenerator ilgen = handler.GetILGenerator();

			//Type[] showParameters = { typeof(object) };
			//MethodInfo simpleShow = typeof(MessageBox).GetMethod("Show", showParameters);

			MethodInfo mi = GetType().GetMethod("DoEventFunction", BindingFlags.NonPublic | BindingFlags.Static);

			//Type[] showParameters = { typeof(String) };
			//MethodInfo simpleShow =
			//    typeof(MessageBox).GetMethod("Show", showParameters);

			ilgen.Emit(OpCodes.Ldarg_1);
			//ilgen.Emit(OpCodes.Ldobj);
			//ilgen.Emit(OpCodes.Ldobj, 
			//ilgen.Emit(OpCodes.Ldstr, "This event handler was constructed at run time.");
			//ilgen.Emit(OpCodes.Call, evtHandler.Method);
			ilgen.Emit(OpCodes.Call, doEvent.Method);
			//ilgen.Emit(OpCodes.Pop);
			ilgen.Emit(OpCodes.Ret);

			// Complete the dynamic method by calling its CreateDelegate 
			// method. Use the "add" accessor to add the delegate to
			// the invocation list for the event. 
			//
			Delegate dEmitted = handler.CreateDelegate(tDelegate);
			addHandler.Invoke(obj, new Object[] { dEmitted });

			//ehi.ActionMethod = Delegate.CreateDelegate(ei.EventHandlerType, ehi, "DoEvent");
			//ei.AddEventHandler(obj, ehi.ActionMethod);

			CurrentContext.RegisteredEventHandlers.Add(ehi);
		}

		private static void DoEventFunction(object e)
		{
			MessageBox.Show("ok" + e.ToString());
		}

		private Type[] GetDelegateParameterTypes(Type d)
		{
			if (d.BaseType != typeof(MulticastDelegate))
				throw new ApplicationException("Not a delegate.");

			MethodInfo invoke = d.GetMethod("Invoke");
			if (invoke == null)
				throw new ApplicationException("Not a delegate.");

			ParameterInfo[] parameters = invoke.GetParameters();
			Type[] typeParameters = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				typeParameters[i] = parameters[i].ParameterType;
			}
			return typeParameters;
		}

		private Type GetDelegateReturnType(Type d)
		{
			if (d.BaseType != typeof(MulticastDelegate))
				throw new ApplicationException("Not a delegate.");

			MethodInfo invoke = d.GetMethod("Invoke");
			if (invoke == null)
				throw new ApplicationException("Not a delegate.");

			return invoke.ReturnType;
		}

		internal void DetachEvent(object obj, EventInfo ei)
		{
			var ehi = CurrentContext.RegisteredEventHandlers.FirstOrDefault(reh =>
				reh.EventInfo == ei && reh.Instance == obj);

			if (ehi != null)
			{
				ehi.EventInfo.RemoveEventHandler(obj, ehi.ActionMethod);

				CurrentContext.RegisteredEventHandlers.Remove(ehi);
			}
		}

		private void DetachAllEvents()
		{
			if (CurrentContext == null) return;

			foreach (EventHandlerInfo handlerInfo in CurrentContext.RegisteredEventHandlers)
			{
				handlerInfo.EventInfo.RemoveEventHandler(handlerInfo.Instance, handlerInfo.ActionMethod);
			}

			CurrentContext.RegisteredEventHandlers.Clear();
		}

		internal FunctionValue GetAttachedEvent(object obj, EventInfo ei)
		{
			var ehi = CurrentContext.RegisteredEventHandlers.FirstOrDefault(reh =>
				reh.EventInfo == ei && reh.Instance == obj);

			return ehi == null ? null : ehi.FunctionValue;
		}

		internal class EventHandlerInfo
		{
			public object Instance { get; set; }
			public EventInfo EventInfo { get; set; }
			public Delegate ActionMethod { get; set; }
			public FunctionValue FunctionValue { get; set; }
			public ScriptRunningMachine Srm { get; set; }

			internal EventHandlerInfo(ScriptRunningMachine srm, object instance,
				EventInfo eventInfo, Delegate delegateMethod, FunctionValue functionValue)
			{
				this.Srm = srm;
				this.Instance = instance;
				this.EventInfo = eventInfo;
				this.ActionMethod = delegateMethod;
				this.FunctionValue = functionValue;
			}

			public void DoEvent(object sender, object arg)
			{
				Srm.InvokeFunction(Instance, FunctionValue, new object[] { arg });
			}
		}

		#endregion

		#region Work Mode
		private MachineWorkMode workMode = MachineWorkMode.Default;

		/// <summary>
		/// Get or set the working mode of script running machine
		/// </summary>
		public MachineWorkMode WorkMode
		{
			get { return workMode; }
			set
			{
				if (workMode != value)
				{
					workMode = value;
					if (WorkModeChanged != null)
					{
						WorkModeChanged(this, null);
					}
				}
			}
		}

		/// <summary>
		/// Event fired when work mode has been changed. (default is Default).
		/// </summary>
		public event EventHandler WorkModeChanged;

		/// <summary>
		/// Allows to access .Net object, type, namespace, etc. directly. (default is false)
		/// </summary>
		internal bool EnableDirectAccess { get { return (workMode & MachineWorkMode.AllowDirectAccess) == MachineWorkMode.AllowDirectAccess; } }

		/// <summary>
		/// Ignore all exception in CLR invoking. (default is true)
		/// </summary>
		internal bool IgnoreCLRExceptions { get { return (workMode & MachineWorkMode.IgnoreCLRExceptions) == MachineWorkMode.IgnoreCLRExceptions; } }

		/// <summary>
		/// Allows ReoScript to auto-import the relation types what may used in other imported type. (default is true)
		/// </summary>
		internal bool AutoImportRelationType { get { return (workMode & MachineWorkMode.AutoImportRelationType) == MachineWorkMode.AutoImportRelationType; } }

		/// <summary>
		/// Allows import .Net namespaces and classes in script using 'import' keyword. (default is fasle)
		/// </summary>
		internal bool EnableImportTypeInScript { get { return (workMode & MachineWorkMode.AllowImportTypeInScript) == MachineWorkMode.AllowImportTypeInScript; } }

		/// <summary>
		/// Allows to auto-bind CLR event. This option needs AllowDirectAccess. (default is false)
		/// </summary>
		internal bool EnableCLREvent { get { return (workMode & MachineWorkMode.AllowCLREventBind) == MachineWorkMode.AllowCLREventBind; } }

		#endregion

		#region Invoke Function
		internal object InvokeFunction(object ownerObject, AbstractFunctionValue funObject, CommonTree argTree)
		{
			return InvokeFunction(ownerObject, funObject, GetParameterList(argTree));
		}

		internal object InvokeFunction(object ownerObject, AbstractFunctionValue funObject, object[] args)
		{
			if (funObject is NativeFunctionValue)
			{
				NativeFunctionValue nativeFun = funObject as NativeFunctionValue;

				return (nativeFun == null || nativeFun.Body == null) ? null
					: nativeFun.Body(this, ownerObject, args);
			}
			else if (funObject is FunctionValue)
			{
				FunctionValue fun = funObject as FunctionValue;

				CallScope newScope = new CallScope(ownerObject, fun);

				if (args != null)
				{
					for (int i = 0; i < fun.Args.Length && i < args.Length; i++)
					{
						string identifier = fun.Args[i];
						newScope[identifier] = args[i];
					}
				}

				CurrentContext.PushCallStack(newScope);

				ReturnNode returnValue = null;

				try
				{
					returnValue = ParseNode(fun.Body) as ReturnNode;
				}
				finally
				{
					CurrentContext.PopVariableStack();
				}

				return returnValue != null ? returnValue.Value : null;
			}
			else
				throw new AWDLRuntimeException("Object is not a function: " + Convert.ToString(funObject));
		}

		/// <summary>
		/// Call a function that existed in Global Object as a property.
		/// This method equals the following script:
		/// if (fun != undefined) fun();
		/// </summary>
		/// <param name="funName">function name</param>
		/// <param name="p">parameters if has</param>
		/// <returns>return value of function</returns>
		public object InvokeFunctionIfExisted(string funName, params object[] p)
		{
			string param = string.Empty;

			if (p != null && p.Length > 0)
			{
				param = string.Join(",", Array.ConvertAll<object, string>(p, _p => Convert.ToString(_p)));
			}

			return Run(
					(!string.IsNullOrEmpty(param))
					? string.Format("if ({0} != undefined) {0}();", funName)
					: string.Format("if ({0} != undefined) {0}({1});", funName, param)
				);
		}
		#endregion

		#region Call Timeout
		//private List<CallWaiting> timeoutQueue = new List<CallWaiting>();
		private List<BackgroundWorker> timeoutList = new List<BackgroundWorker>();

		internal void CallTimeout(object code, int ms)
		{
			if (IsForceStop) return;

			BackgroundWorker bw = new BackgroundWorker() { WorkerSupportsCancellation = true };
			bw.DoWork += (s, e) =>
			{
			  timeoutList.Add(bw);

			  Thread.Sleep(ms);

			  if (code is FunctionValue)
			  {
			    this.ParseNode(((FunctionValue)code).Body);
			  }
			  else if (code is CommonTree)
			  {
			    this.ParseNode(((CommonTree)code));
			  }
			  else
			  {
			    this.CalcExpression(Convert.ToString(code));
			  }

			  timeoutList.Remove(bw);
			};
			bw.RunWorkerAsync();

			//AddAsyncCall(ms, () =>
			//{
			//  Thread.Sleep(ms);

			//  if (code is FunctionValue)
			//  {
			//    this.ParseNode(((FunctionValue)code).Body);
			//  }
			//  else if (code is CommonTree)
			//  {
			//    this.ParseNode(((CommonTree)code));
			//  }
			//  else
			//  {
			//    this.CalcExpression(Convert.ToString(code));
			//  }

			//  return true;
			//});
		}

		private Thread asyncCallThread;

		private List<AsyncCaller> asyncCallerList = new List<AsyncCaller>();

		private int minAsyncCallInterval = 0;

		private void AddAsyncCall(int interval, Func<bool> caller)
		{
			if (minAsyncCallInterval == 0
				|| minAsyncCallInterval > interval)
			{
				minAsyncCallInterval = interval;
			}

			lock (asyncCallerList)
			{
				asyncCallerList.Add(new AsyncCaller(interval, caller));
			}

			if (asyncCallThread == null)
			{
				asyncCallThread = new Thread(AsyncCallLoop);
				asyncCallThread.Start();
			}
		}

		private void AsyncCallLoop()
		{
			while (asyncCallerList.Count > 0)
			{
				// should wait more than 0 ms
				Debug.Assert(minAsyncCallInterval > 0);

				Thread.Sleep(minAsyncCallInterval);

				lock (asyncCallerList)
				{
					for (int i = 0; i < asyncCallerList.Count; i++)
					{
						AsyncCaller caller = asyncCallerList[i];

						if (DateTime.Now > caller.LastCalled.AddMilliseconds(caller.Interval))
						{
							try
							{
								caller.LastCalled = DateTime.Now;
								caller.DoCall();
							}
							catch {
								// error caused in this caller, planned to remove it 
								caller.Finished = true;
							}
						}

						if (caller.Finished)
						{
							asyncCallerList.Remove(caller);
						}
					}

					//asyncCallerList.RemoveAll(ac => ac.Finished);
				}
			}

			asyncCallThread = null;
		}

		internal class AsyncCaller
		{
			public AsyncCaller(int interval, Func<bool> caller)
			{
				this.interval = interval;
				this.lastCalled = DateTime.Now;
				this.action = caller;
				this.finished = false;
			}

			private DateTime lastCalled;

			public DateTime LastCalled
			{
				get { return lastCalled; }
				set { lastCalled = value; }
			}

			private int interval;

			public int Interval
			{
				get { return interval; }
				set { interval = value; }
			}

			private Func<bool> action;

			/// <summary>
			/// Do async calling.
			/// If return true this calling will be removed from timer loop.
			/// </summary>
			/// <returns>return true if calling is finished.</returns>
			public void DoCall()
			{
				if (action != null)
				{
					if (action()) finished = true;
				}
			}

			private bool finished;

			public bool Finished
			{
				get { return finished; }
				set { finished = value; }
			}
		}
		#endregion

		#region Standard Input & Output
		private List<IStdOutputListener> outputListeners;

		/// <summary>
		/// Add a lisenter to get standard output of console.
		/// </summary>
		/// <param name="lisenter">a lisenter to get standard output of console</param>
		public void AddStdOutputListener(IStdOutputListener lisenter)
		{
			if (outputListeners == null) outputListeners = new List<IStdOutputListener>();
			outputListeners.Add(lisenter);
		}

		/// <summary>
		/// Check whether specified listener has been added.
		/// </summary>
		/// <param name="listener">a lisenter to get standard output of console</param>
		/// <returns>true if specified listener has already added.</returns>
		public bool HasStdOutputListener(IStdOutputListener listener)
		{
			return outputListeners == null ? false : outputListeners.Contains(listener);
		}

		/// <summary>
		/// Remove listener from list of lisenters.
		/// </summary>
		/// <param name="lisenter">a lisenter to get standard output of console</param>
		public void RemoveStdOutputListener(IStdOutputListener lisenter)
		{
			if (outputListeners == null) return;
			outputListeners.Remove(lisenter);
		}

		internal void StdOutputWrite(byte[] buf)
		{
			if (outputListeners != null)
			{
				outputListeners.ForEach(ol => ol.Write(buf));
				Application.DoEvents();
			}
		}

		internal void StdOutputWrite(string text)
		{
			if (outputListeners != null)
			{
				outputListeners.ForEach(ol => ol.Write(text));
				Application.DoEvents();
			}
		}

		#endregion

		#region Load & Interpreter
		/// <summary>
		/// Load script library from given stream. 
		/// </summary>
		/// <remarks>setTimeout and setInterval function will be disabled when script load from stream, uri or file.</remarks>
		/// <param name="s">stream to load script</param>
		public void Load(Stream s)
		{
			Load(new ANTLRInputStream(s));
		}

		/// <summary>
		/// Load script library from a specified uri, which may be a remote resource on Internet.
		/// </summary>
		/// <param name="uri">uri to load script</param>
		public void Load(Uri uri)
		{
			using (WebClient c = new WebClient())
			{
				using (Stream stream = c.OpenRead(uri))
				{
					Load(stream);
				}
			}
		}

		/// <summary>
		/// Load script library from a file specified with its fully path name.
		/// </summary>
		/// <param name="path">file path to load script</param>
		public void Load(string path)
		{
			Load(new ANTLRFileStream(path));
		}

		private void Load(ICharStream stream)
		{
			ReoScriptLexer lex = new ReoScriptLexer(stream);
			CommonTokenStream tokens = new CommonTokenStream(lex);
			ReoScriptParser parser = new ReoScriptParser(tokens);

			try
			{
				ParseNode(parser.script().Tree as CommonTree);
			}
			catch (RecognitionException ex)
			{
				throw new AWDLSyntaxErrorException("syntax error near at " + ex.Character + "(" + ex.Line + ": " + ex.CharPositionInLine + ")");
			}
		}

		/// <summary>
		/// Run specified ReoScript script. Note that semicolon is required at end of every line. 
		/// </summary>
		/// <param name="script">statements contained in script will be executed</param>
		/// <returns>value of statement last be executed</returns>
		public object Run(string script)
		{
			return RunCompiledScript(Compile(script));
		}

		public object RunCompiledScript(CompiledScript script)
		{
			// clear ForceStop flag
			IsForceStop = false;

			object v = ParseNode( script.RootNode);
			while (v is IAccess) v = ((IAccess)v).Get();

			return v;
		}

		/// <summary>
		/// Calculate value of ReoScript expression. Only an expression can be properly 
		/// calculated by this method, control statement like if, for, and variable declaration 
		/// passed into method will cause a runtime exception to be thrown. To execute all of
		/// statements you may use Run method.
		/// </summary>
		/// <param name="expression">expression to be calculated</param>
		/// <returns>value of expression</returns>
		public object CalcExpression(string expression)
		{
			ReoScriptLexer exprLex = new ReoScriptLexer(new ANTLRStringStream(expression));
			CommonTokenStream exprTokens = new CommonTokenStream(exprLex);
			ReoScriptParser exprParser = new ReoScriptParser(exprTokens);
			CommonTree t = exprParser.expression().Tree as CommonTree;

			IsForceStop = false;
			object v = ParseNode(t);
			while (v is IAccess) v = ((IAccess)v).Get();

			return v;
		}

		private FunctionDefineNodeParser globalFunctionDefineNodeParser = new FunctionDefineNodeParser();

		public CompiledScript Compile(string script)
		{
			ReoScriptLexer lex = new ReoScriptLexer(new ANTLRStringStream(script));
			CommonTokenStream tokens = new CommonTokenStream(lex);
			ReoScriptParser parser = new ReoScriptParser(tokens);

			try
			{
				// read script and build ASTree
				CommonTree t = parser.script().Tree as CommonTree;

				// scan 1st level and define global functions
				for (int i = 0; i < t.ChildCount; i++)
				{
					if (t.Children[i].Type == ReoScriptLexer.FUNCTION_DEFINE)
					{
						if(t.Children[i].ChildCount>0 && t.Children[i] is CommonTree)
						{
							globalFunctionDefineNodeParser.Parse(t.Children[i] as CommonTree, this);
						}
					}
				}

				return new CompiledScript { RootNode = t };
			}
			catch (RecognitionException ex)
			{
				throw new AWDLSyntaxErrorException("syntax error near at " + ex.Character + "(" + ex.Line + ": " + ex.CharPositionInLine + ")");
			}
		}

		#endregion

		#region Adapter Operations
		private IParserAdapter parserAdapter = new AWDLLogicSyntaxParserAdapter();

		internal IParserAdapter ParserAdapter
		{
			get { return parserAdapter; }
		}

		internal IParserAdapter SelectParserAdapter(IParserAdapter adapter)
		{
			IParserAdapter oldAdapter = this.parserAdapter;
			this.parserAdapter = adapter;
			return oldAdapter;
		}
		#endregion

		#region Node Parsing
		internal object ParseNode(CommonTree t)
		{
			if (t == null || IsForceStop)
			{
				return null;
			}

			INodeParser parser = null;
			if (this.parserAdapter != null && (parser = this.parserAdapter.MatchParser(t)) != null)
			{
				return parser.Parse(t, this);
			}
			else
			{
				//Value v = null;

				switch (t.Type)
				{
					case ReoScriptLexer.THIS:
						return this.CurrentContext.GetCurrentCallScope().ThisObject;

					case ReoScriptLexer.NUMBER_LITERATE:
						return Convert.ToDouble(t.Text);

					case ReoScriptLexer.HEX_LITERATE:
						return Convert.ToInt32(t.Text.Substring(2), 16);

					case ReoScriptLexer.STRING_LITERATE:
						string str = t.ToString();
						str = str.Substring(1, str.Length - 2);
						return new StringObject(str.Replace("\\n", new string(new char[] { '\n' })));

					case ReoScriptLexer.TRUE:
						return true;

					case ReoScriptLexer.FALSE:
						return false;

					case ReoScriptLexer.NULL:
					case ReoScriptLexer.UNDEFINED:
						return null;

					case ReoScriptLexer.OBJECT_LITERAL:
						ObjectValue val = CreateNewObject();

						for (int i = 0; i < t.ChildCount; i += 2)
						{
							object value = ParseNode(t.Children[i + 1] as CommonTree);
							if (value is IAccess) value = ((IAccess)value).Get();

							string identifier = t.Children[i].ToString();
							if (t.Children[i].Type == ReoScriptLexer.STRING_LITERATE)
								identifier = identifier.Substring(1, identifier.Length - 2);

							val[identifier] = value;
						}

						return val;

					case ReoScriptLexer.ARRAY_LITERAL:
						ArrayObject arr = new ArrayObject();
						for (int i = 0; i < t.ChildCount; i++)
						{
							object value = ParseNode(t.Children[i] as CommonTree);
							if (value is IAccess) value = ((IAccess)value).Get();
							arr.List.Add(value);
						}
						return arr;

					case ReoScriptLexer.IDENTIFIER:
						return new VariableAccess(t.Text, this);
				}

				return ParseChildNodes(t);
			}
		}
		internal object ParseChildNodes(CommonTree t)
		{
			object childValue = null;
			if (t.ChildCount > 0)
			{
				foreach (CommonTree child in t.Children)
				{
					childValue = ParseNode(child);

					if (childValue is BreakNode || childValue is ContinueNode || childValue is ReturnNode)
						return childValue;
				}
			}
			return childValue;
		}
		#endregion

		#region Parser Helper
		internal object[] GetParameterList(CommonTree paramsTree)
		{
			int argCount = paramsTree == null ? 0 : paramsTree.ChildCount;
			object[] args = new object[argCount];

			if (argCount > 0)
			{
				for (int i = 0; i < argCount; i++)
				{
					// pass anonymous function
					if (paramsTree.Children[i].Type == ReoScriptLexer.ANONYMOUS_FUNCTION_DEFINE)
					{
						args[i] = (paramsTree.Children[0] as CommonTree).Children[1];
					}
					else
					{
						object val = ParseNode((CommonTree)paramsTree.Children[i]);
						while (val is IAccess) val = ((IAccess)val).Get();

						//if (val is FunctionValue)
						//  args[i] = ((FunctionValue)val).Body;
						//else
						args[i] = val;
					}
				}
			}

			return args;
		}

		internal decimal GetNumericParameter(CommonTree t)
		{
			object value = ParseNode(t);
			while (value is IAccess) value = ((IAccess)value);
			if (!(value is decimal))
			{
				throw new AWDLRuntimeException("parameter must be numeric value.");
			}
			return (decimal)value;
		}

		public static int GetIntValue(object obj)
		{
			return GetIntValue(obj, 0);
		}

		public static int GetIntParam(object[] args, int index, int def)
		{
			if (args.Length <= index)
				return def;
			else
				return GetIntValue(args[index], def);
		}

		public static int GetIntValue(object obj, int def)
		{
			if (obj is int || obj is long)
				return (int)obj;
			else if (obj is double)
				return (int)(double)obj;
			else if (obj is ISyntaxTreeReturn)
			{
				double v = def;
				if (obj is StringObject)
				{
					double.TryParse(((StringObject)obj).String, out v);
				}
				else if (obj is NumberObject)
				{
					v = ((NumberObject)obj).Number;
				}
				return (int)(double)v;
			}
			else
				return def;
		}

		internal static string GetNativeIdentifierName(string identifier)
		{
			return string.IsNullOrEmpty(identifier) ? string.Empty :
				(identifier.Substring(0, 1).ToUpper() + identifier.Substring(1));
		}

		internal object ConvertToCLRType(object value, Type type)
		{
			if (type == typeof(string))
			{
				return Convert.ToString(value);
			}
			else if (type == typeof(int))
			{
				if (value is double)
				{
					return (int)((double)value);
				}
				else
				{
					return Convert.ToInt32(value);
				}
			}
			else if (type == typeof(long))
			{
				if (value is double)
				{
					return (long)((double)value);
				}
				else
					return Convert.ToInt64(value);
			}
			else if (type == typeof(float))
			{
				if (value is double)
				{
					return (float)(double)value;
				}
				else
					return Convert.ToSingle(value);
			}
			else if (value is ObjectValue)
			{
				if (type == typeof(ObjectValue))
				{
					return value;
				}
				else if (EnableDirectAccess)
				{
					object obj;

					if (type.IsArray && value is ArrayObject)
					{
						ArrayObject arrSource = (ArrayObject)value;
						int count = arrSource.List.Count;

						object[] arrTo = Array.CreateInstance(type.GetElementType(), count) as object[];

						for (int i = 0; i < count; i++)
						{
							arrTo[i] = ConvertToCLRType(arrSource.List[i], type.GetElementType());
						}

						obj = arrTo;
					}
					else
					{
						try
						{
							if (type.IsEnum && value is StringObject || value is NumberObject)
							{
								obj = Enum.Parse(type, Convert.ToString(value));
							}
							else
							{
								obj = System.Activator.CreateInstance(type);
							}
						}
						catch (Exception ex)
						{
							throw new AWDLException("cannot convert to .Net object from value: " + value, ex);
						}

						CombileObject(obj, (ObjectValue)value);
					}
					return obj;
				}
				else
					return value;
			}
			else
			{
				return value;
			}
		}

		internal bool IsDirectAccessObject(object obj)
		{
			return !(obj is ISyntaxTreeReturn);

			//var t = CurrentContext.ImportedTypes.FirstOrDefault(it => obj.GetType().Equals(it));

			//if (t != null) return true;

			//if (obj is IDirectAccess) return true;

			//DirectAccessAttribute[] attrs = obj.GetType().GetCustomAttributes(typeof(DirectAccessAttribute), true)
			//  as DirectAccessAttribute[];

			//if (attrs.Length > 0)
			//{
			//  return true;
			//}

			//return false;
		}

		//internal static string GetStringParameter(CommonTree t, ScriptRunningMachine srm)
		//{
		//  Value value = srm.ParseNode(t) as Value;
		//  return value.GetNativeValue().ToString();
		//}

		public static MethodInfo FindCLRMethodAmbiguous(object obj, string methodName, object[] args)
		{
			var q = obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(_q => _q.Name == methodName);

			//MethodInfo method = null;

			foreach (MethodInfo mi in q)
			{
				ParameterInfo[] pi = mi.GetParameters();

				if (pi.Length == args.Length)
				{
					return mi;
				}
				//else
				//{
				//  for (int i = 0; i < args.Length && i < pi.Length; i++)
				//  {
				//    if (pi[i].ParameterType == typeof(string))
				//    {
				//      if(
				//    }
				//  }

				//  // compare every parameters
				//}

			}

			return q == null || q.Count() == 0 ? null : q.First();
		}
		#endregion

	}

	public class CompiledScript
	{
		internal CommonTree RootNode {get;set;}
	}

	/// <summary>
	/// Defines and represents the working mode of ScriptRunningMachine
	/// </summary>
	public enum MachineWorkMode
	{
		/// <summary>
		/// Default working mode
		/// </summary>
		Default = 0 | MachineWorkMode.IgnoreCLRExceptions | MachineWorkMode.AutoImportRelationType,

		/// <summary>
		/// Allows to access .Net object, type, namespace, etc. directly.
		/// </summary>
		AllowDirectAccess = 0x1,

		/// <summary>
		/// Allows to auto-bind CLR event. This option needs AllowDirectAccess.
		/// </summary>
		AllowCLREventBind = 0x2,

		/// <summary>
		/// Allows import .Net namespaces and classes in script using 'import' keyword.
		/// </summary>
		AllowImportTypeInScript = 0x4,

		/// <summary>
		/// Ignore all exception in CLR invoking.
		/// </summary>
		IgnoreCLRExceptions = 0x8,

		/// <summary>
		/// Allows ReoScript to auto-import the relation types that may used in other imported type.
		/// </summary>
		AutoImportRelationType = 0x10,
	}
	#endregion

	#region Standard I/O

	public interface IStdOutputListener
	{
		void Write(string text);
		void WriteLine(string line);
		void Write(byte[] buf);
	}

	public class ConsoleOutputListener : IStdOutputListener
	{
		public void Write(byte[] buf)
		{
			Console.Write(Encoding.ASCII.GetString(buf));
		}

		public void Write(string text)
		{
			Console.Write(text);
		}

		public void WriteLine(string line)
		{
			Console.WriteLine(line);
		}
	}
	#endregion


}
