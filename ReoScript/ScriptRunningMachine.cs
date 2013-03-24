///////////////////////////////////////////////////////////////////////////////
// 
// ReoScript Runtime Machine
// http://www.unvell.com/ReoScript
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
using System.Reflection.Emit;

using Antlr.Runtime;
using Antlr.Runtime.Tree;

using Unvell.ReoScript.Properties;

namespace Unvell.ReoScript
{
	#region Lexer Extension
	public partial class ReoScriptLexer
	{
		public static readonly int HIDDEN = Hidden;

		public const int MAX_TOKENS = 200;
		public const int REPLACED_TREE = MAX_TOKENS - 1;
	}
	#endregion

	#region Syntax Tree Return
	public interface ISyntaxTreeReturn { }

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

		public bool HasOwnProperty(string identifier)
		{
			return Members.ContainsKey(identifier);
		}

		public object GetOwnProperty(string identifier)
		{
			return Members.ContainsKey(identifier) ? Members[identifier] : null;
		}

		public bool RemoveOwnProperty(string identifier)
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

		public string Name { get; set; }

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
			string[] properties = Members.Keys.ToArray<string>();

			//FIXME: manage all of internal property names by SRM
			for (int i = 0; i < properties.Length; i++)
			{
				string key = properties[i];

				if (key != ScriptRunningMachine.KEY___PROTO__
						&& key != ScriptRunningMachine.KEY___ARGS__)
				{
					yield return key;
				}
			}
		}

		#endregion
	}

	class ObjectConstructorFunction : TypedNativeFunctionObject
	{
		private ObjectValue rootPrototype = new ObjectValue();

		public ObjectConstructorFunction()
			: base("Object")
		{
			rootPrototype["hasOwnProperty"] = new NativeFunctionObject("hasOwnProperty", (ctx, owner, args) =>
			{
				ObjectValue ownerObject = owner as ObjectValue;

				if (ownerObject == null || args.Length < 1)
					return false;

				return ownerObject.HasOwnProperty(Convert.ToString(args[0]));
			});

			rootPrototype["removeOwnProperty"] = new NativeFunctionObject("removeOwnProperty", (ctx, owner, args) =>
			{
				ObjectValue ownerObject = owner as ObjectValue;

				if (ownerObject == null || args.Length < 1)
					return false;

				return ownerObject.RemoveOwnProperty(Convert.ToString(args[0]));
			});

			// root object in prototype chain
			this[ScriptRunningMachine.KEY___PROTO__] = rootPrototype;
		}

		public override object Invoke(ScriptContext context, object owner, object[] args)
		{
			ObjectValue obj = owner as ObjectValue;
			return obj == null ? context.CreateNewObject(this, false) : obj;
		}

		public override object CreateObject(ScriptContext context, object[] args)
		{
			return new ObjectValue();
		}

		public override object CreatePrototype(ScriptContext context)
		{
			return rootPrototype;
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
	}
	#endregion
	#region DateTimeValue
	public class DateObject : ObjectValue
	{
		private DateTime dt;

		public DateObject(DateTime value)
		{
			this.dt = value;

			this["getFullYear"] = new NativeFunctionObject("getFullYear", (ctx, owner, args) => { return dt.Year; });
			this["getMonth"] = new NativeFunctionObject("getMonth", (ctx, owner, args) => { return dt.Month; });
			this["getDate"] = new NativeFunctionObject("getDate", (ctx, owner, args) => { return dt.Day; });
			this["getDay"] = new NativeFunctionObject("getDay", (ctx, owner, args) => { return (int)dt.DayOfWeek; });
			this["getHours"] = new NativeFunctionObject("getHours", (ctx, owner, args) => { return dt.Hour; });
			this["getMinutes"] = new NativeFunctionObject("getMinutes", (ctx, owner, args) => { return dt.Minute; });
			this["getSeconds"] = new NativeFunctionObject("getSeconds", (ctx, owner, args) => { return dt.Second; });
			this["getMilliseconds"] = new NativeFunctionObject("getMilliseconds", (ctx, owner, args) => { return dt.Millisecond; });
		}

		public DateObject() :
			this(DateTime.Now)
		{
		}

		public override string ToString()
		{
			return dt.ToLongDateString();
		}
	}
	#endregion
	#region StringValue
	public class StringObject : ObjectValue, IEnumerable
	{
		public string String { get; set; }

		public StringObject()
			: this(string.Empty)
		{
			// this()
		}
		public StringObject(string text)
		{
			String = text;
			this["length"] = new ExternalProperty(() => { return String.Length; }, (v) => { });
		}
		public override bool Equals(object obj)
		{
			return (String == null && obj == null) ? false : String.Equals(Convert.ToString(obj));
		}
		public override int GetHashCode()
		{
			return String.GetHashCode();
		}
		public override string ToString()
		{
			return String;
		}

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			for (int i = 0; i < String.Length; i++)
			{
				yield return String[i];
			}
		}

		#endregion
	}
	class StringConstructorFunction : TypedNativeFunctionObject
	{
		public StringConstructorFunction()
			: base("String")
		{
		}

		public override object Invoke(ScriptContext context, object owner, object[] args)
		{
			StringObject str = owner as StringObject;
			if (str == null) str = context.CreateNewObject(this, false) as StringObject;
			if (args != null && args.Length > 0) str.String = Convert.ToString(args[0]);
			return str;
		}

		public override object CreateObject(ScriptContext context, object[] args)
		{
			return args == null || args.Length <= 0 ? new StringObject() : new StringObject(Convert.ToString(args[0]));
		}

		public override object CreatePrototype(ScriptContext context)
		{
			ScriptRunningMachine srm = context.Srm;
			ObjectValue obj = context.CreateNewObject(srm.BuiltinConstructors.ObjectFunction) as ObjectValue;

			if (obj != null)
			{
				obj["trim"] = new NativeFunctionObject("trim", (ctx, owner, args) =>
				{
					return new StringObject(((StringObject)owner).String = Convert.ToString(owner).Trim());
				});

				obj["indexOf"] = new NativeFunctionObject("indexOf", (ctx, owner, args) =>
				{
					return args.Length == 0 ? -1 : Convert.ToString(owner).IndexOf(Convert.ToString(args[0]));
				});

				obj["lastIndexOf"] = new NativeFunctionObject("lastIndexOf", (ctx, owner, args) =>
				{
					return args.Length == 0 ? -1 : Convert.ToString(owner).LastIndexOf(Convert.ToString(args[0]));
				});

				obj["charAt"] = new NativeFunctionObject("charAt", (ctx, owner, args) =>
				{
					string res = string.Empty;

					if (args.Length > 0)
					{
						int index = ScriptRunningMachine.GetIntParam(args, 0, -1);
						string str = Convert.ToString(owner);

						if (index >= 0 && index < str.Length)
							res = Convert.ToString(str[index]);
					}

					return new StringObject(res);
				});

				obj["charCodeAt"] = new NativeFunctionObject("charCodeAt", (ctx, owner, args) =>
				{
					if (args.Length > 0)
					{
						int index = ScriptRunningMachine.GetIntParam(args, 0, -1);
						string str = Convert.ToString(owner);

						if (index >= 0 && index < str.Length)
							return (int)str[index];
					}

					return NaNValue.Value;
				});

				obj["startsWith"] = new NativeFunctionObject("startsWith", (ctx, owner, args) =>
				{
					return args.Length == 0 ? false : Convert.ToString(owner).StartsWith(Convert.ToString(args[0]));
				});

				obj["endsWith"] = new NativeFunctionObject("endWith", (ctx, owner, args) =>
				{
					return args.Length == 0 ? false : Convert.ToString(owner).EndsWith(Convert.ToString(args[0]));
				});

				obj["repeat"] = new NativeFunctionObject("repeat", (ctx, owner, args) =>
				{
					int count = ScriptRunningMachine.GetIntParam(args, 0, 0);

					string result = string.Empty;

					if (count > 0)
					{
						string str = ((StringObject)owner).String;
						StringBuilder sb = new StringBuilder();
						for (int i = 0; i < count; i++) sb.Append(str);
						result = sb.ToString();
					}

					return new StringObject(result);
				});

				obj["join"] = new NativeFunctionObject("join", (ctx, owner, args) =>
				{
					//TODO
					return new StringObject();
				});
			}

			return obj;
		}
	}

	#endregion

	#region Extension Object
	/// <summary>
	/// Dynamic access properties of an object
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
		/// Create extension property for object with getter and setter method
		/// </summary>
		/// <param name="getter">delegate invoked when value be getted</param>
		/// <param name="setter">delegate invoked when value be setted</param>
		public ExternalProperty(Func<object> getter, Action<object> setter)
		{
			this.Getter = getter;
			this.Setter = setter;
		}

		/// <summary>
		/// Create readonly extension property with only getter method
		/// </summary>
		/// <param name="getter">method invoked when value be getted</param>
		public ExternalProperty(Func<object> getter)
			: this(getter, null) { }

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

	#region Function Object
	//public interface IFunctionObject
	//{
	//  object Invoke(ScriptRunningMachine srm, object owner, object[] args);
	//}
	//public interface IConstructorFunctionObject : IFunctionObject
	//{
	//  object CreateObject(ScriptRunningMachine srm);
	//  ObjectValue Prototype { get; set; }
	//}
	public abstract class AbstractFunctionObject : ObjectValue
	{
		public new abstract string Name { get; set; }
		public virtual object CreateObject(ScriptContext context, object[] args)
		{
			return new ObjectValue();
		}
		public virtual object CreatePrototype(ScriptContext context)
		{
			return context.CreateNewObject(context.Srm.BuiltinConstructors.ObjectFunction, null) as ObjectValue;
		}
	}
	internal class FunctionObject : AbstractFunctionObject
	{
		public override string Name { get; set; }
		public string[] Args { get; set; }
		public CommonTree Body { get; set; }

		public override string ToString()
		{
			return "function " + Name + "() { ... }";
		}
	}
	public class NativeFunctionObject : AbstractFunctionObject
	{
		public override string Name { get; set; }

		public override string ToString()
		{
			return "function " + Name + "() { [native code] }";
		}

		public NativeFunctionObject(string name)
		{
			this.Name = name;
		}

		public NativeFunctionObject(string name, Func<ScriptContext, object, object[], object> body)
		{
			this.Name = name;
			this.Body = body;
		}

		public virtual object Invoke(ScriptContext context, object owner, object[] args)
		{
			return (Body == null ? null : Body(context, owner, args));
		}

		public Func<ScriptContext, object, object[], object> Body { get; set; }
	}
	public class TypedNativeFunctionObject : NativeFunctionObject
	{
		public Type Type { get; set; }

		public Action<ObjectValue> PrototypeBuilder { get; set; }

		public TypedNativeFunctionObject(string name)
			: this(null, name, null, null)
		{
			// this()
		}

		public TypedNativeFunctionObject(Type type, string name)
			: this(type, name, null, null)
		{
			// this()
		}

		public TypedNativeFunctionObject(Type type, string name,
			Func<ScriptContext, object, object[], object> body)
			: this(type, name, body, null)
		{
			// this()
		}

		public TypedNativeFunctionObject(Type type, string name,
			Func<ScriptContext, object, object[], object> body,
			Action<ObjectValue> prototypeBuilder)
			: base(name, body)
		{
			this.Type = type;
			this.PrototypeBuilder = prototypeBuilder;
		}

		public override object CreateObject(ScriptContext context, object[] args)
		{
			try
			{
				object[] cargs = null;

				if (args != null && args.Length > 0)
				{
					ConstructorInfo ci = this.Type.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == args.Length);

					if (ci == null)
					{
						throw new ReoScriptRuntimeException("Cannot create .Net object with incorrect parameters.");
					}

					cargs = new object[args.Length];

					ParameterInfo[] pis = ci.GetParameters();
					for (int i = 0; i < args.Length; i++)
					{
						cargs[i] = context.Srm.ConvertToCLRType(context, args[i], pis[i].ParameterType);
					}
				}

				return System.Activator.CreateInstance(this.Type, BindingFlags.Default, null, cargs, null);
			}
			catch (Exception ex)
			{
				throw new ReoScriptRuntimeException("Error to create .Net instance: " + this.Type.ToString(), ex);
			}
		}

		public override object CreatePrototype(ScriptContext context)
		{
			ObjectValue obj = context.CreateNewObject(context.Srm.BuiltinConstructors.ObjectFunction, null) as ObjectValue;

			if (obj == null) return obj;

			if (PrototypeBuilder != null)
			{
				PrototypeBuilder(obj);
			}

			return obj;
		}
	}
	public class TypedNativeFunctionObject<T> : TypedNativeFunctionObject
	{
		public TypedNativeFunctionObject()
			: this(typeof(T).Name) { }

		public TypedNativeFunctionObject(string name)
			: base(typeof(T), name) { }
	}
	#endregion

	#region Built-in Objects
	#region World Value
	internal class WorldObject : ObjectValue
	{
		#region Built-in functions
		private static readonly NativeFunctionObject __stdout__ = new NativeFunctionObject("__stdout__", (ctx, owner, args) =>
		{
			if (args.Length == 0)
			{
				ctx.Srm.StdOutputWrite(string.Empty);
			}
			else
			{
				ctx.Srm.StdOutputWrite(args[0] == null ? ScriptRunningMachine.KEY_UNDEFINED : Convert.ToString(args[0]));
			}

			if (args.Length > 1)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 1; i < args.Length; i++)
				{
					sb.Append(' ');
					sb.Append(args[0] == null ? ScriptRunningMachine.KEY_UNDEFINED : Convert.ToString(args[i]));
				}

				ctx.Srm.StdOutputWrite(sb.ToString());
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
		#endregion

		public WorldObject()
		{
			this.Name = "Script";

			// built-in native functions
			this[__stdout__.Name] = __stdout__;
			this[__parseInt__.Name] = __parseInt__;
		}
	}
	#endregion

	#region Math
	class MathObject : ObjectValue
	{
		private static readonly Random rand = new Random();

		public MathObject()
		{
			this["random"] = new NativeFunctionObject("random", (ctx, owner, args) =>
			{
				return rand.NextDouble();
			});

			this["round"] = new NativeFunctionObject("round", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else if (args.Length < 2)
					return (Math.Round(Convert.ToDouble(args[0])));
				else
					return (Math.Round(Convert.ToDouble(args[0]), Convert.ToInt32(args[1])));
			});

			this["floor"] = new NativeFunctionObject("floor", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return (Math.Floor(Convert.ToDouble(args[0])));
			});

			this["sin"] = new NativeFunctionObject("sin", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Sin(ScriptRunningMachine.GetDoubleValue(args[0], 0));
			});

			this["cos"] = new NativeFunctionObject("cos", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Cos(ScriptRunningMachine.GetDoubleValue(args[0], 0));
			});

			this["tan"] = new NativeFunctionObject("tan", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Tan(ScriptRunningMachine.GetDoubleValue(args[0], 0));
			});

			this["abs"] = new NativeFunctionObject("abs", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return NaNValue.Value;
				else
					return Math.Abs(ScriptRunningMachine.GetDoubleValue(args[0], 0));
			});

			this["pow"] = new NativeFunctionObject("pow", (ctx, owner, args) =>
			{
				if (args.Length < 2)
					return NaNValue.Value;
				else
					return Math.Pow(ScriptRunningMachine.GetDoubleValue(args[0], 0),
						ScriptRunningMachine.GetDoubleValue(args[1], 0));
			});
		}
	}
	#endregion

	#region Array
	public class ArrayObject : ObjectValue, IEnumerable
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
				() => { return this.Length; },
				(v) =>
				{
					if (v is int || v is long || v is double)
					{
						this.Length = (int)(double)v;
					}
				});
		}

		public int Length
		{
			get
			{
				return list.Count;
			}
			set
			{
				int len = value;

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
			}
		}

		public object this[int index]
		{
			get
			{
				return index >= 0 && index < list.Count ? list[index] : null;
			}
			set
			{
				if (index >= list.Count)
				{
					this.Length = index;
				}

				this.list[index] = value;
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(128);
			sb.Append('[');
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0) sb.Append(", ");
				object val = list[i];
				sb.Append(val == null ? ScriptRunningMachine.KEY_UNDEFINED : Convert.ToString(val));
			}
			sb.Append(']');
			return sb.ToString();
		}

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			for (int i = 0; i < list.Count; i++)
			{
				yield return list[i];
			}
		}

		#endregion
	}
	class ArrayConstructorFunction : TypedNativeFunctionObject
	{
		public ArrayConstructorFunction() :
			base(typeof(ArrayObject), "Array") { }

		public override object Invoke(ScriptContext context, object owner, object[] args)
		{
			return base.Invoke(context, owner, args);
		}

		public override object CreateObject(ScriptContext context, object[] args)
		{
			return new ArrayObject();
		}

		public override object CreatePrototype(ScriptContext context)
		{
			ScriptRunningMachine srm = context.Srm;
			object obj = srm.CreateNewObject(context, srm.BuiltinConstructors.ObjectFunction);

			if (obj is ObjectValue)
			{
				ObjectValue objValue = (ObjectValue)obj;

				objValue["push"] = new NativeFunctionObject("push", (ctx, owner, args) =>
				{
					if (!(owner is ArrayObject)) return null;

					foreach (object v in args)
					{
						((ArrayObject)owner).List.Add(v);
					}
					return null;
				});

				objValue["splice"] = new NativeFunctionObject("splice", (ctx, owner, args) =>
				{
					if (args.Length < 2 || !(owner is ArrayObject)) return null;

					int index = ScriptRunningMachine.GetIntParam(args, 0, 0);
					int howmany = ScriptRunningMachine.GetIntParam(args, 1, 1);

					ArrayObject arr = (ArrayObject)owner;

					arr.List.RemoveRange(index, howmany);

					for (int i = 2; i < args.Length; i++)
						arr.List.Insert(index++, args[i]);

					return null;
				});

				objValue["sort"] = new NativeFunctionObject("sort", (ctx, owner, args) =>
				{
					if (!(owner is ArrayObject)) return null;

					((ArrayObject)owner).List.Sort();
					return null;
				});
			}

			return obj;
		}
	}
	#endregion
	#endregion

	#region Class
	public abstract class XBClass : ISyntaxTreeReturn
	{
		private Dictionary<string, object> Members { get; set; }

		public XBClass()
		{
			Members = new Dictionary<string, object>();
		}

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

		public abstract string TypeName { get; }
	}
	#endregion

	#region Exceptions
	#region AWDLException
	public class ReoScriptException : Exception
	{
		public ReoScriptException(string msg) : base(msg) { }
		public ReoScriptException(string msg, Exception inner) : base(msg, inner) { }
	}
	#endregion
	#region AWDLSyntaxErrorException
	class ReoScriptSyntaxErrorException : ReoScriptException
	{
		public ReoScriptSyntaxErrorException(string msg) : base(msg) { }
		public ReoScriptSyntaxErrorException(string msg, Exception inner) : base(msg, inner) { }
	}
	#endregion
	#region AWDLRuntimeException
	public class ReoScriptRuntimeException : ReoScriptException
	{
		public RuntimePosition Position { get; set; }

		public ReoScriptRuntimeException(string msg, RuntimePosition position)
			: base(msg)
		{
			this.Position = position;
		}

		public ReoScriptRuntimeException(string msg) : base(msg) { }
		public ReoScriptRuntimeException(string msg, Exception inner) : base(msg, inner) { }
	}
	/// <summary>
	/// A position identifies where does the errors happened. Contains char index, line number and full path name. 
	/// </summary>
	public class RuntimePosition
	{
		/// <summary>
		/// Char index
		/// </summary>
		public int CharIndex { get; set; }

		/// <summary>
		/// Line number
		/// </summary>
		public int Line { get; set; }

		/// <summary>
		/// Full path name
		/// </summary>
		public string FilePath { get; set; }

		public RuntimePosition(int charIndex, int line, string filePath)
		{
			this.CharIndex = charIndex;
			this.Line = line;
			this.FilePath = filePath;
		}
	}
	#endregion AWDLRuntimeException
	public class ReoScriptAssertionException : ReoScriptRuntimeException
	{
		public ReoScriptAssertionException(string msg) : base(msg) { }
		public ReoScriptAssertionException(string caused, string excepted)
			: base(string.Format("excepte {0} but {1}", excepted, caused)) { }
	}
	public class ClassNotFoundException : ReoScriptRuntimeException
	{
		public ClassNotFoundException(string msg) : base(msg) { }
		public ClassNotFoundException(string msg, Exception inner) : base(msg, inner) { }
	}
	#endregion

	#region Access
	interface IAccess : ISyntaxTreeReturn
	{
		void Set(object value);
		object Get();
	}

	abstract class AccessValue : ISyntaxTreeReturn, IAccess
	{
		protected ScriptContext Context { get; set; }
		protected ScriptRunningMachine Srm { get; set; }

		public AccessValue(ScriptRunningMachine srm, ScriptContext ctx)
		{
			this.Srm = srm;
			this.Context = ctx;
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
		public VariableAccess(ScriptRunningMachine srm, ScriptContext ctx, string identifier)
			: base(srm, ctx)
		{
			this.identifier = identifier;
			Scope = ctx.GetVariableScope(identifier);

			if (Scope == null && ctx.GlobalObject[identifier] != null)
			{
				GlobalObject = ctx.GlobalObject;
			}
		}
		public CallScope Scope { get; set; }
		public ObjectValue GlobalObject { get; set; }
		#region Access Members
		public override void Set(object value)
		{
			if (Scope == null)
			{
				Context.GlobalObject[identifier] = value;
			}
			else
			{
				Scope[identifier] = value;
			}
			//if (Scope != null)
			//{
			//  Scope[identifier] = value;
			//}
			//else if (GlobalObject != null)
			//{
			//  GlobalObject[identifier] = value;
			//}
			//else
			//{
			//  // new variable declare to global object 
			//  srm.CurrentContext[identifier] = value;
			//}
		}
		public override object Get()
		{
			if (identifier == ScriptRunningMachine.GLOBAL_VARIABLE_NAME)
			{
				return Context.GlobalObject;
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
		public ArrayAccess(ScriptRunningMachine srm, ScriptContext ctx, object array, int index)
			: base(srm, ctx)
		{
			this.array = array;
			this.index = index;
		}
		#region Access Members
		public override void Set(object value)
		{
			if ((array is ArrayObject))
			{
				((ArrayObject)array)[index] = value;
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
			else if (Srm.EnableDirectAccess && Srm.IsDirectAccessObject(array))
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
				return ((ArrayObject)array)[index];
			}
			else if (array is StringObject)
			{
				string str = Convert.ToString(array);
				return index >= 0 && index < str.Length ? new StringObject(str[index].ToString()) :
					new StringObject();
			}
			else if (Srm.EnableDirectAccess && Srm.IsDirectAccessObject(array))
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
		public PropertyAccess(ScriptRunningMachine srm, ScriptContext ctx, object obj, string identifer)
			: base(srm, ctx)
		{
			this.obj = obj;
			this.identifier = identifer;
		}
		#region Access Members
		public override void Set(object value)
		{
			PropertyAccessHelper.SetProperty(Srm, Context, obj, identifier, value);
		}
		public override object Get()
		{
			return PropertyAccessHelper.GetProperty(Srm, obj, identifier);
		}
		#endregion
	}

	sealed class PropertyAccessHelper
	{
		internal static void SetProperty(ScriptRunningMachine srm, ScriptContext context, object target, string identifier, object value)
		{
			// in DirectAccess mode and object is accessable directly
			if (srm.EnableDirectAccess && srm.IsDirectAccessObject(target))
			{
				string memberName = ScriptRunningMachine.GetNativeIdentifierName(identifier);

				// if value is anonymous function, try to attach CLR event
				if (value is FunctionObject)
				{
					if (srm.EnableCLREvent)
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
							if (target is Control && ((Control)target).InvokeRequired)
							{
								((Control)target).Invoke((MethodInvoker)(() =>
								{
									pi.SetValue(target, srm.ConvertToCLRType(context, value, pi.PropertyType), null);
								}));
							}
							else
							{
								pi.SetValue(target, srm.ConvertToCLRType(context, value, pi.PropertyType), null);
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
			if (target is ObjectValue)
			{
				ObjectValue objValue = (ObjectValue)target;
				object val = objValue[identifier];
				if (val is ExternalProperty)
				{
					return (((ExternalProperty)val).GetNativeValue());
				}
				else
				{
					object propertyValue = objValue[identifier];

					// if there is no found, get property from its prototype
					if (propertyValue == null && objValue.HasOwnProperty(ScriptRunningMachine.KEY___PROTO__))
					{
						propertyValue = PropertyAccessHelper.GetProperty(srm,
							objValue.GetOwnProperty(ScriptRunningMachine.KEY___PROTO__), identifier);
					}

					return propertyValue;
				}
			}
			else if (srm.EnableDirectAccess && srm.IsDirectAccessObject(target))
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
					}
				}
			}

			return null;
		}
	}
	#endregion

	#endregion

	#region Parsers
	#region Parser Interface
	interface INodeParser
	{
		object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx);
	}
	#endregion

	#region Import Statement
	class ImportNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			if (t.Children[0].Type == ReoScriptLexer.STRING_LITERATE)
			{
				string codeFile = t.Children[0].ToString();
				codeFile = codeFile.Substring(1, codeFile.Length - 2);

				string path = Path.GetFullPath(codeFile);

				srm.ImportCodeFile(path);
			}
			else if (srm.EnableImportTypeInScript)
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
		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext context)
		{
			object lastValue = null;
			for (int i = 1; i < t.ChildCount; i++)
			{
				var identifier = t.Children[0].ToString();
				var value = srm.ParseNode((CommonTree)t.Children[i], context);

				if (value is IAccess) value = ((IAccess)value).Get();

				// declare variable in current call stack
				if (srm.IsInGlobalScope(context))
				{
					srm[identifier] = value;
				}
				else
				{
					context.GetCurrentCallScope()[identifier] = value;
				}

				lastValue = value;
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext context)
		{
			if (t.ChildCount == 1)
			{
				return null;
			}

			IAccess access = srm.ParseNode((CommonTree)t.Children[0], context) as IAccess;
			if (access == null)
			{
				if (srm.IsInGlobalScope(context))
				{
					access = new PropertyAccess(srm, context, srm.GlobalObject, t.Children[0].Text);
				}
			}

			CommonTree expr = t.ChildCount > 1 ? (CommonTree)t.Children[1] : null;

			object value = null;
			if (expr != null)
			{
				value = srm.ParseNode(expr, context);
			}

			if (value is IAccess) value = ((IAccess)value).Get();

			if (access != null)
			{
				access.Set(value);
			}
			else if (!srm.IsInGlobalScope(context))
			{
				context[t.Children[0].Text] = value;
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			object left = srm.ParseNode((CommonTree)t.Children[0], ctx);
			if (left is IAccess) left = ((IAccess)left).Get();

			object right = srm.ParseNode((CommonTree)t.Children[1], ctx);
			if (right is IAccess) right = ((IAccess)right).Get();

			return Calc(left, right, srm, ctx);
		}

		public abstract object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context);

		#endregion
	}
	abstract class MathExpressionOperatorParser : ExpressionOperatorNodeParser
	{
		public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
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
		public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
		{
			if ((left == null && right == null)
				|| left == null || right == null
				|| left == NaNValue.Value || right == NaNValue.Value)
				return NaNValue.Value;

			if ((left is double || left is int || left is long || left is float)
				&& (right is double || right is int || right is long || right is float))
			{
				return Convert.ToDouble(left) + Convert.ToDouble(right);
			}
			else if (left is string || right is string
				|| left is StringObject || right is StringObject)
			{
				return srm.CreateNewObject(context, srm.BuiltinConstructors.StringFunction,
					new object[] { string.Empty + left + right });
			}
			else if (left.GetType() == typeof(ObjectValue) && right.GetType() == typeof(ObjectValue))
			{
				ObjectValue obj = srm.CreateNewObject(context);
				srm.CombineObject(context, obj, ((ObjectValue)left));
				srm.CombineObject(context, obj, ((ObjectValue)right));
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

		public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
		{
			if ((left is int || left is double || left is long || left is float)
				&& (right is int || right is double || right is long || right is float))
			{
				return (Convert.ToInt64(left) & Convert.ToInt64(right));
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

		public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
		{
			if ((left is int || left is long || left is float || left is double)
				&& (right is int || right is long || right is float || right is double))
			{
				return (Convert.ToInt64(left) | Convert.ToInt64(right));
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

		public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
		{
			if (left is long && right is long)
			{
				return ((long)left ^ (long)right);
			}
			else if (left is ObjectValue && right is ObjectValue)
			{
				ObjectValue leftObj = (ObjectValue)left;
				ObjectValue rightObj = (ObjectValue)right;

				ObjectValue resultObject = srm.CreateNewObject(context);

				foreach (string key in leftObj)
				{
					if (rightObj.HasOwnProperty(key))
					{
						resultObject[key] = leftObj[key];
					}
				}

				return resultObject;
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

		public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
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

		public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			string oprt = t.Children[0].ToString();
			object value = srm.ParseNode((CommonTree)t.Children[1], ctx);

			// get value
			while (value is IAccess) value = ((IAccess)value).Get();

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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			CommonTree target = (CommonTree)t.Children[0];
			IAccess access = srm.ParseNode((CommonTree)t.Children[0], ctx) as IAccess;
			if (access == null)
			{
				throw new ReoScriptRuntimeException("only property, indexer, and variable can be used as increment or decrement statement.");
			}

			object oldValue = access.Get();
			if (oldValue == null)
			{
				oldValue = 0;
			}

			if (!(oldValue is double || oldValue is int || oldValue is long))
			{
				throw new ReoScriptRuntimeException("only interger can be used as increment or decrement statement.");
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			CommonTree target = (CommonTree)t.Children[0];
			IAccess access = srm.ParseNode((CommonTree)t.Children[0], ctx) as IAccess;
			if (access == null)
			{
				throw new ReoScriptRuntimeException("only property, indexer, and variable can be used as increment or decrement statement.");
			}
			object oldValue = access.Get();
			if (oldValue == null)
			{
				oldValue = 0;
			}

			if (!(oldValue is double || oldValue is int || oldValue is long))
			{
				throw new ReoScriptRuntimeException("only interger can be used as increment or decrement statement.");
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			object value = srm.ParseNode((CommonTree)t.Children[0], ctx);
			if (!(value is bool))
			{
				throw new ReoScriptRuntimeException("only boolean expression can be used for conditional expression.");
			}
			bool condition = (bool)value;
			return condition ? srm.ParseNode((CommonTree)t.Children[1], ctx)
				: srm.ParseNode((CommonTree)t.Children[2], ctx);
		}

		#endregion
	}
	#endregion
	#region Relation Expression Operator
	abstract class RelationExpressionOperatorNodeParser : ExpressionOperatorNodeParser
	{
		#region INodeParser Members
		public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
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
				if ((left is int || left is long || left is float || left is double)
				&& (right is int || right is long || right is float || right is double))
				{
					return Convert.ToDouble(left) == Convert.ToDouble(right);
				}
				else
				{
					return left.Equals(right);
				}
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
			if ((left is int || left is long || left is float || left is double)
			&& (right is int || right is long || right is float || right is double))
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
			if ((left is int || left is long || left is float || left is double)
			&& (right is int || right is long || right is float || right is double))
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
			if ((left is int || left is long || left is float || left is double)
				&& (right is int || right is long || right is float || right is double))
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
			if ((left is int || left is long || left is double || left is float)
				&& (right is int || right is long || right is double || right is float))
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
	#region Boolean &&
	class BooleanAndNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			object left = srm.ParseNode((CommonTree)t.Children[0], ctx);
			if (left is IAccess) left = ((IAccess)left).Get();

			if (left == null || !(left is bool))
				return false;

			bool leftBool = (bool)left;
			if (!leftBool) return false;

			object right = srm.ParseNode((CommonTree)t.Children[1], ctx);
			if (right is IAccess) right = ((IAccess)right).Get();

			if (right == null || !(right is bool))
				return false;

			bool rightBool = (bool)right;
			return rightBool;
		}

		#endregion
	}
	#endregion
	#region Boolean ||
	class BooleanOrNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			object left = srm.ParseNode((CommonTree)t.Children[0], ctx);
			if (left is IAccess) left = ((IAccess)left).Get();

			if (left == null || !(left is bool))
				return false;

			bool leftBool = (bool)left;
			if (leftBool) return true;

			object right = srm.ParseNode((CommonTree)t.Children[1], ctx);
			if (right is IAccess) right = ((IAccess)right).Get();

			if (right == null || !(right is bool))
				return false;

			bool rightBool = (bool)right;
			return rightBool;
		}

		#endregion
	}
	#endregion
	#region If Else
	class IfStatementNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			object value = srm.ParseNode((CommonTree)t.Children[0], ctx);
			if (value is IAccess) value = ((IAccess)value).Get();

			if (!(value is bool))
			{
				return false;
				//throw new AWDLRuntimeException("only boolean expression can be used as test condition.");
			}
			bool condition = (bool)value;
			if (condition)
			{
				return srm.ParseNode((CommonTree)t.Children[1], ctx);
			}
			else if (t.ChildCount == 3)
			{
				return srm.ParseNode((CommonTree)t.Children[2], ctx);
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			if (t.ChildCount == 0) return null;

			object source = srm.ParseNode(t.Children[0] as CommonTree, ctx);
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
				else if (caseTree.Type == ReoScriptLexer.RETURN)
				{
					if (doParse) return srm.ParseNode(caseTree, ctx);
				}
				else if (caseTree.Type == ReoScriptLexer.SWITCH_CASE_ELSE)
				{
					defaultCaseLine = i;
				}
				else if (caseTree.Type == ReoScriptLexer.SWITCH_CASE)
				{
					if (caseTree.ChildCount > 0)
					{
						object target = srm.ParseNode(caseTree.Children[0] as CommonTree, ctx);
						if (target is IAccess) target = ((IAccess)target).Get();

						if ((bool)equalsParser.Calc(source, target, srm, ctx))
						{
							doParse = true;
						}
					}
				}
				else if (doParse)
				{
					srm.ParseNode(caseTree, ctx);
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			CommonTree forInit = (CommonTree)t.Children[0];
			srm.ParseChildNodes(forInit, ctx);

			CommonTree condition = (CommonTree)t.Children[1];
			CommonTree forIterator = (CommonTree)t.Children[2];
			CommonTree body = (CommonTree)t.Children[3];

			while (true)
			{
				object conditionValue = srm.ParseNode(condition, ctx) as object;
				if (conditionValue != null)
				{
					bool? booleanValue = conditionValue as bool?;

					if (booleanValue == null)
					{
						throw new ReoScriptRuntimeException("only boolean expression can be used as test condition.");
					}
					else if (!((bool)booleanValue))
					{
						return null;
					}
				}

				object result = srm.ParseNode(body, ctx);
				if (result is BreakNode)
				{
					return null;
				}

				srm.ParseNode(forIterator, ctx);
			}
		}

		#endregion
	}
	#endregion
	#region Foreach
	class ForEachStatementNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext context)
		{
			string varName = t.Children[0].ToString();

			CallScope scope = null;

			if (t.ChildCount > 3 && t.Children[3].Type == ReoScriptLexer.TYPE
				&& !srm.IsInGlobalScope(context))
			{
				scope = context.GetCurrentCallScope();
			}

			// retrieve target object
			object nativeObj = srm.ParseNode(t.Children[1] as CommonTree, context);
			if (nativeObj is IAccess) nativeObj = ((IAccess)nativeObj).Get();

			if (nativeObj is IEnumerable)
			{
				IEnumerator iterator = ((IEnumerable)nativeObj).GetEnumerator();

				while (iterator.MoveNext())
				{
					// prepare key
					if (scope == null)
					{
						srm[varName] = iterator.Current;
					}
					else
					{
						scope[varName] = iterator.Current;
					}

					// prepare iterator
					CommonTree body = t.Children[2] as CommonTree;

					// call iterator and terminal loop if break
					object result = srm.ParseNode(body, context);
					if (result is BreakNode)
					{
						return null;
					}
					else if (result is ReturnNode)
					{
						return result;
					}
				}
			}

			return null;
		}

		#endregion
	}
	#endregion
	#region Break
	class BreakNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
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

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			object v = null;

			if (t.ChildCount > 0)
			{
				v = srm.ParseNode((CommonTree)t.Children[0], ctx);
				if (v is IAccess) v = ((IAccess)v).Get();
			}

			// TODO: make ReturnNode single instance
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
		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext context)
		{
			string funName = t.Children[0].Text;
			CommonTree paramsTree = t.Children[1] as CommonTree;

			FunctionObject fun = CreateAndInitFunction(srm, context, funName, paramsTree, (CommonTree)t.Children[2]);

			if (srm.IsInGlobalScope(context))
			{
				context.GlobalObject[fun.Name] = fun;
			}
			else
			{
				context.GetCurrentCallScope()[fun.Name] = fun;
			}

			return null;
		}
		#endregion

		internal static FunctionObject CreateAndInitFunction(ScriptRunningMachine srm, ScriptContext context,
			string funName, CommonTree paramsTree, CommonTree body)
		{
			FunctionObject fun = srm.CreateNewObject(context, srm.BuiltinConstructors.FunctionFunction) as FunctionObject;

			if (fun == null) return null;

			ObjectValue prototype = srm.CreateNewObject(context, srm.BuiltinConstructors.ObjectFunction) as ObjectValue;
			prototype[ScriptRunningMachine.KEY___PROTO__] = fun[ScriptRunningMachine.KEY___PROTO__];

			fun[ScriptRunningMachine.KEY_PROTOTYPE] = prototype;

			string[] identifiers = new string[paramsTree.ChildCount];

			for (int i = 0; i < identifiers.Length; i++)
			{
				identifiers[i] = paramsTree.Children[i].ToString();
			}

			fun.Name = funName;
			fun.Args = identifiers;
			fun.Body = body;

			return fun;
		}
	}
	#endregion
	#region Anonymous Function
	class AnonymousFunctionNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			return FunctionDefineNodeParser.CreateAndInitFunction(srm, ctx, string.Empty,
				((CommonTree)t.Children[0]), (CommonTree)t.Children[1]);
		}

		#endregion
	}

	#endregion
	#region Function Call
	class FunctionCallNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext context)
		{
			object funObj = null;
			object ownerObj = null;

			// local-function call
			if (t.Children[0].Type == ReoScriptLexer.IDENTIFIER)
			{
				string funName = t.Children[0].ToString();
				funObj = context[funName];
			}
			else
			{
				// other objects call
				funObj = srm.ParseNode(t.Children[0] as CommonTree, context);

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
										(t.ChildCount <= 1 ? null : t.Children[1] as CommonTree), context);

								MethodInfo mi = ScriptRunningMachine.FindCLRMethodAmbiguous(ownerObj,
									ScriptRunningMachine.GetNativeIdentifierName(methodName), args);

								if (mi != null)
								{
									ParameterInfo[] paramTypeList = mi.GetParameters();

									try
									{
										object[] convertedArgs = new object[args.Length];
										for (int i = 0; i < convertedArgs.Length; i++)
										{
											convertedArgs[i] = srm.ConvertToCLRType(context, args[i], paramTypeList[i].ParameterType);
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
						throw new ReoScriptRuntimeException(string.Format("{0} has no method '{1}'", ownerObj, methodName));
					}
				}
				else
				{
					while (funObj is IAccess) funObj = ((IAccess)funObj).Get();
				}
			}

			if (funObj == null)
			{
				throw new ReoScriptRuntimeException("Function is not defined: " + t.Children[0].ToString());
			}

			if (ownerObj == null) ownerObj = context.ThisObject;

			if (!(funObj is AbstractFunctionObject))
			{
				throw new ReoScriptRuntimeException("Object is not a function: " + Convert.ToString(funObj));
			}

			CommonTree argTree = t.ChildCount < 2 ? null : t.Children[1] as CommonTree;
			return srm.InvokeFunction(context, ownerObj, ((AbstractFunctionObject)funObj), srm.GetParameterList(argTree, context));
		}

		#endregion
	}
	#endregion
	#region Create
	class CreateObjectNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext context)
		{
			// combiled construct
			if (t.ChildCount > 0 && t.Children[0].Type == ReoScriptLexer.COMBINE_OBJECT)
			{
				CommonTree combileTree = ((CommonTree)t.Children[0]);
				ObjectValue combileObj = srm.ParseNode(combileTree.Children[1] as CommonTree, context) as ObjectValue;

				object createdObj = Parse(combileTree.Children[0] as CommonTree, srm, context);
				srm.CombineObject(context, createdObj, combileObj);
				return createdObj;
			}

			CommonTree tempTree = t.Children[0] as CommonTree;
			CommonTree constructTree = t;

			// need a depth variable to remember the depth of construct calling
			int committedDepth = 0, depth = 0;

			// find construct calling
			while (tempTree != null && tempTree.Children != null)
			{
				if (tempTree.Type == ReoScriptLexer.FUNCTION_CALL)
				{
					constructTree = tempTree;
					committedDepth += depth;
				}

				tempTree = tempTree.Children[0] as CommonTree;
				depth++;
			}

			if (constructTree == null) throw new ReoScriptRuntimeException("unexpected end in new operation.");

			// get constructor if it is need to retrieve from other Accessors
			object constructorValue = srm.ParseNode(constructTree.Children[0] as CommonTree, context);

			// get identifier of constructor
			string constructorName = constructTree.Children[0].Type == ReoScriptLexer.IDENTIFIER
				? constructTree.Children[0].Text : ScriptRunningMachine.KEY_UNDEFINED;

			if (constructorValue is IAccess) constructorValue = ((IAccess)constructorValue).Get();

			if (constructorValue == null && srm.EnableDirectAccess)
			{
				Type type = srm.GetImportedTypeFromNamespaces(constructorName);
				if (type != null)
				{
					constructorValue = new TypedNativeFunctionObject(type, type.Name);
				}
			}

			if (constructorValue == null)
			{
				throw new ReoScriptRuntimeException("Constructor function not found: " + constructorName);
			}

			if (!(constructorValue is AbstractFunctionObject))
			{
				throw new ReoScriptRuntimeException("Constructor is not a function type: " + constructorName);
			}
			else
			{
				// call constructor
				AbstractFunctionObject funObj = (AbstractFunctionObject)constructorValue;

				CommonTree argTree = (constructTree == null || constructTree.ChildCount < 2) ? null : constructTree.Children[1] as CommonTree;
				object[] args = srm.GetParameterList(argTree, context);

				object obj = srm.CreateNewObject(context, funObj, args);

				// committedDepth > 0 means there is some primaryExpressions are remaining.
				// replace current construction tree and call srm to execute the remaining.
				if (committedDepth > 0)
				{
					CommonTreeAdaptor ad = new CommonTreeAdaptor();
					CommonTree newTree = ad.DupTree(t) as CommonTree;

					int d = 0;
					CommonTree ct = newTree.Children[0] as CommonTree;
					while (d++ < committedDepth - 1)
					{
						ct = ct.Children[0] as CommonTree;
					}

					// replace construction tree with created object
					ct.ReplaceChildren(0, 0, new ReplacedCommonTree(obj));

					// drop the construction tree topmost node [CREATE]
					newTree = newTree.Children[0] as CommonTree;

					// execute remained nodes than construction tree
					return srm.ParseNode(newTree, context);
				}
				else
					return obj;
			}
		}

		#endregion
	}
	#endregion
	#region ArrayAccess
	class ArrayAccessNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext context)
		{
			object value = srm.ParseNode((CommonTree)t.Children[0], context);
			if (value is IAccess) value = ((IAccess)value).Get();

			if (value == null)
			{
				throw new ReoScriptRuntimeException("Attempt to access an array or object is null or undefined.",
					new RuntimePosition(t.CharPositionInLine, t.Line, string.Empty));
			}

			object indexValue = srm.ParseNode((CommonTree)t.Children[1], context);
			if (indexValue is IAccess) indexValue = ((IAccess)indexValue).Get();

			if (indexValue is StringObject || indexValue is string)
			{
				// index access for object
				return new PropertyAccess(srm, context, value, Convert.ToString(indexValue));
			}
			else
			{
				// index access for array
				int index = ScriptRunningMachine.GetIntValue(indexValue);
				return new ArrayAccess(srm, context, value, index);
			}
		}

		#endregion
	}
	#endregion
	#region PropertyAccess
	class PropertyAccessNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			object value = null;

			value = srm.ParseNode((CommonTree)t.Children[0], ctx);
			if (value is IAccess) value = ((IAccess)value).Get();

			if (value == null) throw new ReoScriptRuntimeException(
				"Attempt to access property of null or undefined object" +
				((t.Children[0].Type == ReoScriptLexer.IDENTIFIER)
				? (": " + t.Children[0].ToString()) : "."),
				new RuntimePosition(t.CharPositionInLine, t.Line, string.Empty));

			if (!(value is ObjectValue))
			{
				if (value is ISyntaxTreeReturn)
				{
					throw new ReoScriptRuntimeException(
						string.Format("Attempt to access an object '{0}' that is not Object type.", value.ToString()),
						new RuntimePosition(t.CharPositionInLine, t.Line, string.Empty));
				}
				else if (!srm.EnableDirectAccess)
				{
					throw new ReoScriptRuntimeException(string.Format(
						"Attempt to access an object '{0}' that is not in Object type. If want to access a .Net object, set WorkMode to enable DirectAccess.", 
						value.ToString()),
						new RuntimePosition(t.CharPositionInLine, t.Line, string.Empty));
				}
			}

			return new PropertyAccess(srm, ctx, value, t.Children[1].ToString());
		}

		#endregion
	}
	#endregion
	#region Delete Property
	class DeletePropertyNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			t = t.Children[0] as CommonTree;
			if (t == null) return false;

			if (t.Type == ReoScriptLexer.IDENTIFIER)
			{
				string identifier = t.Text;
				if (ctx.GlobalObject.HasOwnProperty(identifier))
				{
					ctx.GlobalObject.RemoveOwnProperty(identifier);
					return true;
				}
			}
			else if (t.Type == ReoScriptLexer.PROPERTY_ACCESS)
			{
				if (t.Children[1].Type != ReoScriptLexer.IDENTIFIER)
				{
					throw new ReoScriptRuntimeException("delete keyword requires an identifier to delete property from object.",
						new RuntimePosition(t.CharPositionInLine, t.Line, string.Empty));
				}

				object owner = srm.ParseNode(t.Children[0] as CommonTree, ctx);
				if (owner is IAccess) owner = ((IAccess)owner).Get();

				if (owner == null)
				{
					string msg = "Attmpt to delete property from null or undefined object";

					if (t.Children[0].Type == ReoScriptLexer.IDENTIFIER)
					{
						msg += ": " + t.Text;
					}
					else
					{
						msg += ".";
					}

					throw new ReoScriptRuntimeException(msg,
						new RuntimePosition(t.CharPositionInLine, t.Line, string.Empty));
				}

				ObjectValue ownerObject = (ObjectValue)owner;

				if (ownerObject != null)
				{
					ownerObject.RemoveOwnProperty(t.Children[1].Text);
					return true;
				}
			}

			return false;
		}

		#endregion
	}

	#endregion
	#region Typeof
	class TypeofNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			object obj = srm.ParseNode(t.Children[0] as CommonTree, ctx);
			if (obj is IAccess) obj = ((IAccess)obj).Get();

			if (obj is ObjectValue)
			{
				return ((ObjectValue)obj).Name;
			}
			else if (obj is ISyntaxTreeReturn)
			{
				return null;
			}
			else
			{
				return obj.GetType().Name;
			}
		}

		#endregion
	}
	#endregion

	#region Class
	class ClassDefineNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			XBClass cls = default(XBClass);
			return cls;
		}

		#endregion
	}

	class ClassMemberDefineNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
	#endregion
	#region Tag
	class TagNodeParser : INodeParser
	{
		#region INodeParser Members

		public object Parse(CommonTree t, ScriptRunningMachine srm, ScriptContext ctx)
		{
			AbstractFunctionObject funObject = srm.GetClass(((CommonTree)t.Children[0]).Children[0].ToString());
			if (funObject == null) return null;

			ObjectValue obj = srm.CreateNewObject(ctx, funObject) as ObjectValue;

			if (obj != null)
			{
				CommonTree attrTree = t.Children[1] as CommonTree;
				for (int i = 0; i < attrTree.ChildCount; i++)
				{
					CommonTree attr = attrTree.Children[i] as CommonTree;

					object val = srm.ParseNode(attr.Children[1] as CommonTree, ctx);
					if (val is IAccess) val = ((IAccess)val).Get();

					PropertyAccessHelper.SetProperty(srm, ctx, obj, attr.Children[0].ToString(), val);
				}

				CallScope scope = new CallScope(obj, funObject);
				ctx.PushCallStack(scope);

				try
				{
					for (int i = 2; i < t.ChildCount; i++)
					{
						CommonTree tagStmt = t.Children[i] as CommonTree;

						if (tagStmt.Type == ReoScriptLexer.TAG)
						{
							srm.InvokeFunctionIfExisted(obj, "appendChild", srm.ParseNode(tagStmt, ctx));
						}
						else
						{
							srm.ParseNode(tagStmt, ctx);
						}
					}
				}
				finally
				{
					ctx.PopCallStack();
				}
			}

			return obj;
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
		private static readonly INodeParser[] definedParser = new INodeParser[ReoScriptLexer.MAX_TOKENS];

		static AWDLLogicSyntaxParserAdapter()
		{
			#region Generic Parsers
			definedParser[ReoScriptLexer.IMPORT] = new ImportNodeParser();
			definedParser[ReoScriptLexer.LOCAL_DECLARE_ASSIGNMENT] = new DeclarationNodeParser();
			definedParser[ReoScriptLexer.ASSIGNMENT] = new AssignmentNodeParser();
			definedParser[ReoScriptLexer.IF_STATEMENT] = new IfStatementNodeParser();
			definedParser[ReoScriptLexer.FOR_STATEMENT] = new ForStatementNodeParser();
			definedParser[ReoScriptLexer.FOREACH_STATEMENT] = new ForEachStatementNodeParser();
			definedParser[ReoScriptLexer.SWITCH] = new SwitchCaseStatementNodeParser();
			definedParser[ReoScriptLexer.FUNCTION_CALL] = new FunctionCallNodeParser();
			definedParser[ReoScriptLexer.FUNCTION_DEFINE] = new FunctionDefineNodeParser();
			definedParser[ReoScriptLexer.ANONYMOUS_FUNCTION] = new AnonymousFunctionNodeParser();
			definedParser[ReoScriptLexer.BREAK] = new BreakNodeParser();
			definedParser[ReoScriptLexer.CONTINUE] = new ContinueNodeParser();
			definedParser[ReoScriptLexer.RETURN] = new ReturnNodeParser();
			definedParser[ReoScriptLexer.CREATE] = new CreateObjectNodeParser();
			definedParser[ReoScriptLexer.ARRAY_ACCESS] = new ArrayAccessNodeParser();
			definedParser[ReoScriptLexer.PROPERTY_ACCESS] = new PropertyAccessNodeParser();
			definedParser[ReoScriptLexer.DELETE] = new DeletePropertyNodeParser();
			definedParser[ReoScriptLexer.TYPEOF] = new TypeofNodeParser();

			definedParser[ReoScriptLexer.TAG] = new TagNodeParser();
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
	public class ScriptContext
	{
		internal ScriptContext(ScriptRunningMachine srm, AbstractFunctionObject function)
		{
#if DEBUG
			Debug.Assert(srm != null);
			Debug.Assert(srm.GlobalObject != null);
			Debug.Assert(function != null);
#endif

			this.GlobalObject = srm.GlobalObject;
			this.Srm = srm;
			PushCallStack(new CallScope(this.GlobalObject, function));
		}

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

		public ScriptRunningMachine Srm { get; set; }

		/// <summary>
		/// Global object (Global objects in all contexts should be unique object)
		/// </summary>
		internal ObjectValue GlobalObject { get; set; }

		/// <summary>
		/// Not supported
		/// </summary>
		internal ObjectValue WithObject { get; set; }

		#region Variable Stack
		internal object this[string identifier]
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

		internal CallScope GetCurrentCallScope()
		{
			return callStack.Count > 0 ? callStack.Peek() : null;
		}

		internal CallScope GetVariableScope(string identifier)
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

		internal void PushCallStack(CallScope scope)
		{
			callStack.Push(scope);
		}

		internal void PopCallStack()
		{
			if (callStack.Count > 0) callStack.Pop();
		}
		#endregion

		public ObjectValue CreateNewObject()
		{
			return Srm.CreateNewObject(this);
		}

		public object CreateNewObject(AbstractFunctionObject funObject)
		{
			return Srm.CreateNewObject(this, funObject, true);
		}

		public object CreateNewObject(AbstractFunctionObject funObject, bool invokeConstructor)
		{
			return Srm.CreateNewObject(this, funObject, invokeConstructor, null);
		}

		public object CreateNewObject(AbstractFunctionObject funObject, object[] args)
		{
			return Srm.CreateNewObject(this, funObject, true, args);
		}

		public object CreateNewObject(AbstractFunctionObject funObject, bool invokeConstructor, object[] args)
		{
			return Srm.CreateNewObject(this, funObject, invokeConstructor);
		}
	}

	internal class CallScope
	{
		//TODO: entry char index, line, file path

		public object ThisObject { get; set; }
		public AbstractFunctionObject CurrentFunction { get; set; }

		public CallScope(object thisObject, AbstractFunctionObject funObject)
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
	public sealed class ScriptRunningMachine
	{
		#region Const & Keywords

		/// <summary>
		/// Keyword to undefined script object
		/// </summary>
		internal static readonly string KEY_UNDEFINED = "undefined";

		internal static readonly string KEY_PROTOTYPE = "prototype";

		internal static readonly string KEY___PROTO__ = "__proto__";

		internal static readonly string KEY_CONSTRUCTOR = "constructor";

		internal static readonly string KEY___ARGS__ = "__args__";

		/// <summary>
		/// Global variable name
		/// </summary>
		internal static readonly string GLOBAL_VARIABLE_NAME = "script";

		#endregion

		#region Constructor

		/// <summary>
		/// Specifies what features can be supported by SRM.
		/// After modify this value call the Reset method to apply changes.
		/// </summary>
		public CoreFeatures CoreFeatures { get; set; }

		/// <summary>
		/// Construct SRM with Standard feature support.
		/// </summary>
		public ScriptRunningMachine()
			: this(CoreFeatures.StandardFeatures)
		{
			//this();
		}

		/// <summary>
		/// Construct SRM with specified feature support.
		/// </summary>
		/// <param name="coreFeatures">Specifies what features can be supported by SRM.</param>
		public ScriptRunningMachine(CoreFeatures coreFeatures)
		{
			this.CoreFeatures = coreFeatures;

			Reset();
		}

		~ScriptRunningMachine()
		{
			DetachAllEvents();

			try
			{
				if (asyncCallThread != null) asyncCallThread.Abort();
			}
			catch { }
		}

		#endregion

		#region Context
		internal BuiltinConstructors BuiltinConstructors = new BuiltinConstructors();

		//internal ScriptContext CurrentContext { get; set; }

		//internal object RetrieveVariable(string identifier)
		//{
		//  object obj = CurrentContext[identifier];
		//  return obj == null ? this[identifier] : obj;
		//}

		/// <summary>
		/// Reset current context to clear all variables and restart running machine.
		/// </summary>
		public void Reset()
		{
			ForceStop();

			while (IsRunning)
				Thread.Sleep(100);

			// detach all attched CLR events
			DetachAllEvents();

			// reset imported
			ImportedNamespace.Clear();
			ImportedTypes.Clear();

			// reset machine status
			isForceStop = false;
			BuiltinConstructors = new BuiltinConstructors();

			// renew global object
			GlobalObject = new WorldObject();

			// renew context
			//ScriptContext sc = new ScriptContext(GlobalObject);
			//sc.PushCallStack(new CallScope(GlobalObject, entryFunction));

			// load core library
			BuiltinConstructors.ApplyToScriptRunningMachine(this);
			LoadCoreLibraries();

			if (Resetted != null) Resetted(this, null);
		}

		internal void LoadCoreLibraries()
		{
			using (MemoryStream ms = new MemoryStream(Resources.lib_core))
			{
				Load(ms);
			}
		}

		#endregion

		#region Global Variable
		internal WorldObject GlobalObject { get; set; }

		/// <summary>
		/// Set value as a property to the global object. Value name specified by
		/// identifier. After this, the value can be used in script like a normal 
		/// variable.
		/// </summary>
		/// <param name="identifier">name to variable</param>
		/// <param name="obj">value of variable</param>
		public void SetGlobalVariable(string identifier, object obj)
		{
			// if object is function, prepare its prototype 
			if (obj is AbstractFunctionObject)
			{
				AbstractFunctionObject functionObj = (AbstractFunctionObject)obj;

				if (functionObj[KEY_PROTOTYPE] == null)
				{
					functionObj[KEY_PROTOTYPE] = functionObj.CreatePrototype(
						new ScriptContext(this, entryFunction));
				}
			}

			GlobalObject[identifier] = obj;
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
			return GlobalObject[identifier];
		}

		/// <summary>
		/// Delete a specified global value.
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public bool RemoveGlobalVariable(string identifier)
		{
			return GlobalObject.RemoveOwnProperty(identifier);
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
				return GetGlobalVariable(identifier);
			}
			set
			{
				SetGlobalVariable(identifier, value);
			}
		}

		/// <summary>
		/// Dummy function object for most outside code scope.
		/// </summary>
		internal static readonly FunctionObject entryFunction = new FunctionObject()
		{
			Name = "__entry__",
		};

		internal bool IsInGlobalScope(ScriptContext context)
		{
			return context.GetCurrentCallScope().CurrentFunction == entryFunction;
		}

		#endregion

		#region Object Management

		/// <summary>
		/// Create a new object instance 
		/// </summary>
		/// <returns>object is created</returns>
		public ObjectValue CreateNewObject()
		{
			return CreateNewObject(new ScriptContext(this, entryFunction), BuiltinConstructors.ObjectFunction) as ObjectValue;
		}

		internal ObjectValue CreateNewObject(ScriptContext context)
		{
			return CreateNewObject(context, BuiltinConstructors.ObjectFunction) as ObjectValue;
		}

		internal object CreateNewObject(ScriptContext context, AbstractFunctionObject funObject)
		{
			return CreateNewObject(context, funObject, true);
		}

		internal object CreateNewObject(ScriptContext context, AbstractFunctionObject funObject, bool invokeConstructor)
		{
			return CreateNewObject(context, funObject, invokeConstructor, null);
		}

		internal object CreateNewObject(ScriptContext context, AbstractFunctionObject funObject, object[] constructArguments)
		{
			return CreateNewObject(context, funObject, true, constructArguments);
		}

		internal object CreateNewObject(ScriptContext context, AbstractFunctionObject constructor, bool invokeConstructor, object[] constructArguments)
		{
			object obj = null;

			if (constructor is NativeFunctionObject)
			{
				obj = ((NativeFunctionObject)constructor).CreateObject(context, constructArguments);
			}

			if (obj == null) obj = new ObjectValue();

			if (obj is ObjectValue)
			{
				ObjectValue objValue = obj as ObjectValue;

				objValue.Name = constructor.Name;

				// get prototype from constructor
				object prototype = constructor[KEY_PROTOTYPE];

				// create prototype if not existed
				objValue[KEY___PROTO__] = prototype;
			}

			if (invokeConstructor)
			{
				InvokeFunction(context, obj, constructor, constructArguments);
			}

			if (obj != null && NewObjectCreated != null)
			{
				NewObjectCreated(this, new ReoScriptObjectEventArgs(obj, constructor));
			}

			return obj;
		}

		#endregion

		#region CLR Type Import
		private List<ScriptRunningMachine.EventHandlerInfo> registeredEventHandlers = new List<ScriptRunningMachine.EventHandlerInfo>();

		internal List<ScriptRunningMachine.EventHandlerInfo> RegisteredEventHandlers
		{
			get { return registeredEventHandlers; }
			set { registeredEventHandlers = value; }
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

		/// <summary>
		/// Import a .Net type into script context. This method will creates a constructor function
		/// which named by type's name and stored as property in global object. Note that if there 
		/// is an object named type's name does exists in global object then it will be overwritten.
		/// </summary>
		/// <param name="type">type to be added into script context</param>
		public void ImportType(Type type)
		{
			if (ImportedTypes.Contains(type))
			{
				ImportedTypes.Remove(type);
			}

			ImportedTypes.Add(type);

			SetGlobalVariable(type.Name, new TypedNativeFunctionObject(type, type.Name));
		}

		/// <summary>
		/// Import a namespace into script context
		/// </summary>
		/// <param name="name">namespace to be registered into script context</param>
		public void ImportNamespace(string name)
		{
			if (name.EndsWith("*")) name = name.Substring(0, name.Length - 1);
			if (name.EndsWith(".")) name = name.Substring(0, name.Length - 1);

			if (!ImportedNamespace.Contains(name))
			{
				ImportedNamespace.Add(name);
			}
		}

		internal Type GetImportedTypeFromNamespaces(string typeName)
		{
			Type type = null;

			foreach (string ns in ImportedNamespace)
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

		internal void CombineObject(ScriptContext context, object target, ObjectValue source)
		{
			foreach (string key in source)
			{
				// FIXME: get member by PropertyAccessHelper.GetProperty?
				PropertyAccessHelper.SetProperty(this, context, target, key, source[key]);
			}
		}

		#endregion

		#region CLR Event

		internal void AttachEvent(ScriptContext context, object obj, EventInfo ei, FunctionObject functionValue)
		{
			// remove last attached event to sample object
			DetachEvent(obj, ei);

			EventHandlerInfo ehi = new EventHandlerInfo(this, context, obj, ei, null, functionValue);
			Action<object> doEvent = (e) => { InvokeFunction(context, obj, functionValue, new object[] { e }); };

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

			RegisteredEventHandlers.Add(ehi);
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

			RegisteredEventHandlers.Add(ehi);
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
			var ehi = RegisteredEventHandlers.FirstOrDefault(reh =>
				reh.EventInfo == ei && reh.Instance == obj);

			if (ehi != null)
			{
				ehi.EventInfo.RemoveEventHandler(obj, ehi.ActionMethod);

				RegisteredEventHandlers.Remove(ehi);
			}
		}

		private void DetachAllEvents()
		{
			foreach (EventHandlerInfo handlerInfo in RegisteredEventHandlers)
			{
				handlerInfo.EventInfo.RemoveEventHandler(handlerInfo.Instance, handlerInfo.ActionMethod);
			}

			RegisteredEventHandlers.Clear();
		}

		internal FunctionObject GetAttachedEvent(object obj, EventInfo ei)
		{
			var ehi = RegisteredEventHandlers.FirstOrDefault(reh =>
				reh.EventInfo == ei && reh.Instance == obj);

			return ehi == null ? null : ehi.FunctionValue;
		}

		internal class EventHandlerInfo
		{
			public object Instance { get; set; }
			public EventInfo EventInfo { get; set; }
			public Delegate ActionMethod { get; set; }
			public FunctionObject FunctionValue { get; set; }
			public ScriptRunningMachine Srm { get; set; }
			public ScriptContext Context { get; set; }

			internal EventHandlerInfo(ScriptRunningMachine srm, ScriptContext context, object instance,
				EventInfo eventInfo, Delegate delegateMethod, FunctionObject functionValue)
			{
				this.Srm = srm;
				this.Context = context;
				this.Instance = instance;
				this.EventInfo = eventInfo;
				this.ActionMethod = delegateMethod;
				this.FunctionValue = functionValue;
			}

			public void DoEvent(object sender, object arg)
			{
				Srm.InvokeFunction(Context, Instance, FunctionValue, new object[] { arg });
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
		public object InvokeAbstractFunction(object ownerObject, AbstractFunctionObject funObject, object[] args)
		{
			ScriptContext context = new ScriptContext(this, entryFunction);
			return InvokeFunction(context, ownerObject, funObject, args);
		}

		internal object InvokeFunction(ScriptContext context, object ownerObject, AbstractFunctionObject funObject, object[] args)
		{
			if (funObject is NativeFunctionObject)
			{
				NativeFunctionObject nativeFun = funObject as NativeFunctionObject;

				return nativeFun.Invoke(context, ownerObject, args);
				//return (nativeFun == null || nativeFun.BodyFull == null) ? null
				//  : nativeFun.Invoke(this, context, ownerObject, args);
			}
			else if (funObject is FunctionObject)
			{
				FunctionObject fun = funObject as FunctionObject;

				CallScope newScope = new CallScope(ownerObject, fun);

				if (args != null)
				{
					for (int i = 0; i < fun.Args.Length && i < args.Length; i++)
					{
						string identifier = fun.Args[i];
						newScope[identifier] = args[i];
					}
				}

				ArrayObject argumentArray = CreateNewObject(context, BuiltinConstructors.ArrayFunction) as ArrayObject;
				if (argumentArray != null)
				{
					if (args != null) argumentArray.List.AddRange(args);
					newScope[KEY___ARGS__] = argumentArray;
				}

				context.PushCallStack(newScope);

				ReturnNode returnValue = null;

				try
				{
					returnValue = ParseNode(fun.Body, context) as ReturnNode;
				}
				finally
				{
					context.PopCallStack();
				}

				return returnValue != null ? returnValue.Value : null;
			}
			else
				throw new ReoScriptRuntimeException("Object is not a function: " + Convert.ToString(funObject));
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
			return InvokeFunctionIfExisted(GlobalObject, funName, p);
		}

		public object InvokeFunctionIfExisted(object owner, string funName, params object[] p)
		{
			AbstractFunctionObject fun = PropertyAccessHelper.GetProperty(this, owner, funName)
				as AbstractFunctionObject;

			return fun != null ? InvokeFunction(new ScriptContext(this, entryFunction), owner, fun, p) : null;
		}
		#endregion

		#region Async Calling
		private bool isForceStop = false;

		/// <summary>
		/// Flag to specify whether current executing is requesting to force stop
		/// </summary>
		public bool IsForceStop { get { return isForceStop; } }

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
				//return asyncCallThread != null;
				//return timeoutList.Count > 0;
			}
		}

		/// <summary>
		/// Force interrupt current execution.
		/// </summary>
		public void ForceStop()
		{
			isForceStop = true;

			//asyncCallerList.Clear();

			//if (asyncCallThread != null)
			//{
			//  asyncCallThread.Abort();
			//  asyncCallThread = null;
			//}

			lock (timeoutList)
			{
				for (int i = 0; i < timeoutList.Count; i++)
				{
					AsyncBackgroundWorker bw = timeoutList[i];

					try
					{
						if (bw != null)
						{
							timeoutList.Remove(bw);

							bw.CancelAsync();
							bw.Dispose();
							bw = null;
						}
					}
					catch { }
				}
			}
		}

		private List<AsyncBackgroundWorker> timeoutList = new List<AsyncBackgroundWorker>();

		private long asyncCallingCount = 0;

		internal long AsyncCall(object code, int ms, bool forever, object[] args)
		{
			if (IsForceStop) return 0;

			//AddAsyncCall(ms, () =>
			//{
			//  Thread.Sleep(ms);

			//  if (code is FunctionObject)
			//  {
			//    this.ParseNode(((FunctionObject)code).Body);
			//  }
			//  else if (code is CommonTree)
			//  {
			//    this.ParseNode(((CommonTree)code));
			//  }
			//  else
			//  {
			//    //FIXME: should merge Run and CalcExpression
			//    string codeStr = Convert.ToString(code).TrimEnd();
			//    if (codeStr.EndsWith(";") || codeStr.EndsWith("}"))
			//    {
			//      this.Run(Convert.ToString(codeStr));
			//    }
			//    else
			//    {
			//      this.CalcExpression(Convert.ToString(codeStr));
			//    }
			//  }

			//  return true;
			//});

			AsyncBackgroundWorker bw = new AsyncBackgroundWorker()
			{
				WorkerSupportsCancellation = true,
				ScriptContext = new ScriptContext(this, entryFunction),
			};
			
			bw.Id = ++asyncCallingCount;

			bw.DoWork += (s, e) =>
			{
				try
				{
					do
					{
						DateTime dt = DateTime.Now.AddMilliseconds(ms);

						while (DateTime.Now < dt)
						{
							if (isForceStop || bw.CancellationPending) break;
							Thread.Sleep(10);
						}

						if (isForceStop || bw.CancellationPending)
						{
							break;
						}
						else
						{
							if (code is FunctionObject)
							{
								this.InvokeFunction(bw.ScriptContext, GlobalObject, code as FunctionObject, args);
							}
							else if (code is CommonTree)
							{
								this.ParseNode(((CommonTree)code), bw.ScriptContext);
							}
							else
							{
								this.CalcExpression(Convert.ToString(code));
							}
						}

						if (!forever)
						{
							timeoutList.Remove(bw);
						}
					} while (forever);
				}
				catch(Exception ex)
				{
					throw ex;
				}
			};

			timeoutList.Add(bw);
			bw.RunWorkerAsync();

			return bw.Id;
		}

		class AsyncBackgroundWorker : BackgroundWorker
		{
			public long Id { get; set; }

			public ScriptContext ScriptContext { get; set; }
		}

		internal bool CancelAsyncCall(long id)
		{
			for (int i = 0; i < timeoutList.Count; i++)
			{
				if (timeoutList[i].Id == id)
				{
					timeoutList[i].CancelAsync();
					timeoutList.RemoveAt(i);
					return true;
				}
			}

			return false;
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

			if (minAsyncCallInterval > 10) minAsyncCallInterval = 10;

			asyncCallerList.Add(new AsyncCaller(interval, caller));

			if (asyncCallThread == null)
			{
				asyncCallThread = new Thread(AsyncCallLoop);
				asyncCallThread.Start();
			}
		}

		private void AsyncCallLoop()
		{
			DateTime dt = DateTime.Now;

			while (asyncCallerList.Count > 0)
			{
				// should wait more than 0 ms
				Debug.Assert(minAsyncCallInterval > 0 && minAsyncCallInterval <= 100);

				Thread.Sleep(minAsyncCallInterval);

				for (int i = 0; i < asyncCallerList.Count; i++)
				{
					AsyncCaller caller = asyncCallerList[i];

					double check = (DateTime.Now - caller.LastCalled).TotalMilliseconds;
					Debug.WriteLine("check = " + check);

					if (check > caller.Interval)
					{
						try
						{
							caller.LastCalled = DateTime.Now;
							caller.DoCall();
						}
						catch
						{
							// error caused in this caller, planned to remove it 
							caller.Finished = true;
						}

						Debug.WriteLine((DateTime.Now - dt).Milliseconds);
						dt = DateTime.Now;
					}

					if (caller.Finished)
					{
						asyncCallerList.Remove(caller);
					}
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

		#region Load & Run
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
				ParseNode(parser.script().Tree as CommonTree, new ScriptContext(this, entryFunction));
			}
			catch (RecognitionException ex)
			{
				throw new ReoScriptSyntaxErrorException("syntax error near at " + ex.Character + "(" + ex.Line + ": " + ex.CharPositionInLine + ")");
			}
		}

		/// <summary>
		/// Run specified ReoScript script. (Note that semicolon is required at end of line)
		/// </summary>
		/// <param name="script">script to execute</param>
		/// <returns>result of last exected statement</returns>
		public object Run(string script)
		{
			ScriptContext context = new ScriptContext(this, entryFunction);
			return RunCompiledScript(Compile(script, context), context);
		}

		/// <summary>
		/// Run compiled script
		/// </summary>
		/// <param name="script">compiled script object to execute</param>
		/// <returns>result of last exected statement</returns>
		public object RunCompiledScript(CompiledScript script)
		{
			return RunCompiledScript(script, new ScriptContext(this, entryFunction));
		}

		public object RunCompiledScript(CompiledScript script, ScriptContext context)
		{
			// clear ForceStop flag
			isForceStop = false;

			if (script.RootNode == null) return null;

			// parse syntax tree
			object v = ParseNode(script.RootNode, context);

			// retrieve value from accessor
			if (v is IAccess) v = ((IAccess)v).Get();

			// retrieve value from ReturnNode
			if (v is ReturnNode) v = ((ReturnNode)v).Value;

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

			isForceStop = false;
			object v = ParseNode(t, new ScriptContext(this, entryFunction));
			while (v is IAccess) v = ((IAccess)v).Get();

			return v;
		}

		private FunctionDefineNodeParser globalFunctionDefineNodeParser = new FunctionDefineNodeParser();

		/// <summary>
		/// Compile script (Pre-parse script to improve executing speed). 
		/// </summary>
		/// <param name="script">script to compile</param>
		/// <returns>A compiled script object</returns>
		public CompiledScript Compile(string script)
		{
			return Compile(script, new ScriptContext(this, entryFunction));
		}

		public CompiledScript Compile(string script, ScriptContext context)
		{
			ReoScriptLexer lex = new ReoScriptLexer(new ANTLRStringStream(script));
			CommonTokenStream tokens = new CommonTokenStream(lex);
			ReoScriptParser parser = new ReoScriptParser(tokens);

			// read script and build ASTree
			CommonTree t = parser.script().Tree;

			if (t != null)
			{
				// scan 1st level and define global functions
				for (int i = 0; i < t.ChildCount; i++)
				{
					if (t.Children[i].Type == ReoScriptLexer.FUNCTION_DEFINE)
					{
						if (t.Children[i].ChildCount > 0 && t.Children[i] is CommonTree)
						{
							globalFunctionDefineNodeParser.Parse(t.Children[i] as CommonTree, this, context);
						}
					}
				}
			}

			return new CompiledScript { RootNode = t };
		}

		#endregion

		#region Node Parsing
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

		internal object ParseNode(CommonTree t, ScriptContext ctx)
		{
			if (t == null || IsForceStop)
			{
				return null;
			}

			if (t is CommonErrorNode)
			{
				CommonErrorNode errorNode = (CommonErrorNode)t;

				string msg = null;

				if (errorNode.trappedException != null)
				{
					RecognitionException re = (RecognitionException)errorNode.trappedException;

					msg = string.Format("syntax error at {0} in line {1}", re.CharPositionInLine, re.Line);

					if (re is MismatchedTokenException)
					{
						MismatchedTokenException mte = (MismatchedTokenException)re;
						msg += string.Format(", expect {0}", mte.TokenNames[mte.Expecting]);
					}
					else if (re is NoViableAltException)
					{
						NoViableAltException nvae = (NoViableAltException)re;
						msg += ", no viable alt";
					}
				}

				if (msg == null)
				{
					msg = "syntax error";
				}

				throw new ReoScriptSyntaxErrorException(msg);
			}

			INodeParser parser = null;
			if (this.parserAdapter != null && (parser = this.parserAdapter.MatchParser(t)) != null)
			{
				return parser.Parse(t, this, ctx);
			}
			else
			{
				//Value v = null;

				switch (t.Type)
				{
					case ReoScriptLexer.THIS:
						return ctx.GetCurrentCallScope().ThisObject;

					case ReoScriptLexer.NUMBER_LITERATE:
						return Convert.ToDouble(t.Text);

					case ReoScriptLexer.HEX_LITERATE:
						return Convert.ToInt32(t.Text.Substring(2), 16);

					case ReoScriptLexer.BINARY_LITERATE:
						return Convert.ToInt32(t.Text.Substring(2), 2);

					case ReoScriptLexer.STRING_LITERATE:
						string str = t.ToString();
						str = str.Substring(1, str.Length - 2);
						object strObj = CreateNewObject(ctx, BuiltinConstructors.StringFunction,
							new object[] { str.Replace("\\n", new string(new char[] { '\n' })) });
						return strObj;

					case ReoScriptLexer.TRUE:
						return true;

					case ReoScriptLexer.FALSE:
						return false;

					case ReoScriptLexer.NULL:
					case ReoScriptLexer.UNDEFINED:
						return null;

					case ReoScriptLexer.OBJECT_LITERAL:
						ObjectValue val = CreateNewObject(ctx);

						for (int i = 0; i < t.ChildCount; i += 2)
						{
							object value = ParseNode(t.Children[i + 1] as CommonTree, ctx);
							if (value is IAccess) value = ((IAccess)value).Get();

							string identifier = t.Children[i].ToString();
							if (t.Children[i].Type == ReoScriptLexer.STRING_LITERATE)
								identifier = identifier.Substring(1, identifier.Length - 2);

							val[identifier] = value;
						}

						return val;

					case ReoScriptLexer.ARRAY_LITERAL:
						ArrayObject arr = CreateNewObject(ctx, BuiltinConstructors.ArrayFunction) as ArrayObject;

						if (arr == null) return arr;

						for (int i = 0; i < t.ChildCount; i++)
						{
							object value = ParseNode(t.Children[i] as CommonTree, ctx);
							if (value is IAccess) value = ((IAccess)value).Get();
							arr.List.Add(value);
						}
						return arr;

					case ReoScriptLexer.IDENTIFIER:
						return new VariableAccess(this, ctx, t.Text);

					case ReoScriptLexer.REPLACED_TREE:
						return ((ReplacedCommonTree)t).Object;
				}

				return ParseChildNodes(t, ctx);
			}
		}
		internal object ParseChildNodes(CommonTree t, ScriptContext ctx)
		{
			object childValue = null;
			if (t.ChildCount > 0)
			{
				foreach (CommonTree child in t.Children)
				{
					childValue = ParseNode(child, ctx);

					if (childValue is BreakNode || childValue is ContinueNode || childValue is ReturnNode)
						return childValue;
				}
			}
			return childValue;
		}
		#endregion

		#region Parser Helper
		internal object[] GetParameterList(CommonTree paramsTree, ScriptContext ctx)
		{
			int argCount = paramsTree == null ? 0 : paramsTree.ChildCount;
			object[] args = new object[argCount];

			if (argCount > 0)
			{
				for (int i = 0; i < argCount; i++)
				{
					object val = ParseNode((CommonTree)paramsTree.Children[i], ctx);
					if (val is IAccess) val = ((IAccess)val).Get();

					args[i] = val;
				}
			}

			return args;
		}

		internal decimal GetNumericParameter(CommonTree t, ScriptContext ctx)
		{
			object value = ParseNode(t, ctx);
			while (value is IAccess) value = ((IAccess)value);
			if (!(value is decimal))
			{
				throw new ReoScriptRuntimeException("parameter must be numeric.");
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

		public static long GetLongValue(object obj)
		{
			return GetLongValue(obj, 0);
		}

		public static long GetLongParam(object[] args, int index, long def)
		{
			if (args.Length <= index)
				return def;
			else
				return GetLongValue(args[index], def);
		}

		public static long GetLongValue(object obj, long def)
		{
			if (obj is int || obj is long)
				return (long)obj;
			else if (obj is double)
				return (long)(double)obj;
			else if (obj is ISyntaxTreeReturn)
			{
				long v = def;
				if (obj is StringObject)
				{
					long.TryParse(((StringObject)obj).String, out v);
				}
				else if (obj is NumberObject)
				{
					v = (long)((NumberObject)obj).Number;
				}
				return v;
			}
			else
				return def;
		}

		public static float GetFloatValue(object obj)
		{
			return GetFloatValue(obj, 0);
		}

		public static float GetFloatValue(object obj, float def)
		{
			if (obj is float)
				return (float)obj;
			else if (obj is int || obj is long)
				return (float)obj;
			else if (obj is double)
				return (float)(double)obj;
			else if (obj is ISyntaxTreeReturn)
			{
				float v = def;
				if (obj is StringObject)
				{
					float.TryParse(((StringObject)obj).String, out v);
				}
				else if (obj is NumberObject)
				{
					v = (float)((NumberObject)obj).Number;
				}
				return v;
			}
			else
				return def;
		}

		public static double GetDoubleValue(object obj)
		{
			return GetDoubleValue(obj, 0);
		}

		public static double GetDoubleValue(object obj, double def)
		{
			if (obj is double)
				return (double)obj;
			else if (obj is NumberObject)
			{
				return ((NumberObject)obj).Number;
			}
			else if (obj is int || obj is long || obj is float)
			{
				return (double)(float)obj;
			}
			else if (obj is ISyntaxTreeReturn)
			{
				double v = def;
				if (obj is StringObject)
				{
					double.TryParse(((StringObject)obj).String, out v);
				}
				return v;
			}
			else
				return def;
		}

		internal static string GetNativeIdentifierName(string identifier)
		{
			return string.IsNullOrEmpty(identifier) ? string.Empty :
				(identifier.Substring(0, 1).ToUpper() + identifier.Substring(1));
		}

		internal object ConvertToCLRType(ScriptContext context, object value, Type type)
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
							arrTo[i] = ConvertToCLRType(context, arrSource.List[i], type.GetElementType());
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
							throw new ReoScriptException("cannot convert to .Net object from value: " + value, ex);
						}

						CombineObject(context, obj, (ObjectValue)value);
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

		#region Events
		internal event EventHandler<ReoScriptObjectEventArgs> NewObjectCreated;
		//internal event EventHandler<ReoScriptObjectEventArgs> PropertyDeleted;
		public event EventHandler Resetted;
		#endregion

		#region Namespace & Class & CodeFile
		private readonly Dictionary<string, AbstractFunctionObject> classDefines
			= new Dictionary<string, AbstractFunctionObject>();

		public bool HasClassRegistered(string name)
		{
			return classDefines.ContainsKey(name)
				|| classDefines.ContainsKey(ScriptRunningMachine.GetNativeIdentifierName(name));
		}

		internal AbstractFunctionObject GetClass(string name)
		{
			string upperName = ScriptRunningMachine.GetNativeIdentifierName(name);

			if (classDefines.ContainsKey(name))
				return classDefines[name];
			else if (classDefines.ContainsKey(upperName))
				return classDefines[upperName];
			else
				throw new ClassNotFoundException("Class Not Found: " + upperName);
		}

		public void RegisterClass(NativeFunctionObject cls)
		{
			string name = cls.Name;

			if (string.IsNullOrEmpty(name))
			{
				name = cls.GetType().Name;
			}

			if (classDefines.ContainsKey(name))
			{
				throw new ReoScriptException(string.Format("Class named '{0}' has been defined.", cls.Name));
			}

			// if object is function, prepare its prototype 
			if (cls[KEY_PROTOTYPE] == null)
			{
				cls[KEY_PROTOTYPE] = cls.CreatePrototype(new ScriptContext(this, entryFunction));
			}

			classDefines[name] = cls;
		}

		private static readonly List<string> importedCodeFiles = new List<string>();

		internal void ImportCodeFile(string fullPath)
		{
			if (!importedCodeFiles.Contains(fullPath))
			{
				importedCodeFiles.Add(fullPath);

				Load(fullPath);
			}
		}

		#endregion
	}

	#region CompiledScript

	public class CompiledScript
	{
		internal CommonTree RootNode { get; set; }
	}

	#endregion

	#region built-in Object Constructors
	internal class BuiltinConstructors
	{
		internal ObjectConstructorFunction ObjectFunction;
		internal StringConstructorFunction StringFunction;
		internal TypedNativeFunctionObject FunctionFunction;
		internal TypedNativeFunctionObject NumberFunction;
		internal TypedNativeFunctionObject DateFunction;
		internal ArrayConstructorFunction ArrayFunction;

		public BuiltinConstructors()
		{
			ObjectFunction = new ObjectConstructorFunction();
			StringFunction = new StringConstructorFunction();

			FunctionFunction = new TypedNativeFunctionObject
				(typeof(FunctionObject), "Function", (ctx, owner, args) =>
				{
					FunctionObject fun = owner as FunctionObject;
					//TOOD: create function from string
					if (fun == null) fun = ctx.CreateNewObject(FunctionFunction, false) as FunctionObject;
					return fun;
				});

			NumberFunction = new TypedNativeFunctionObject
				(typeof(NumberObject), "Number", (ctx, owner, args) =>
				{
					NumberObject num = owner as NumberObject;
					if (num == null) num = ctx.CreateNewObject(NumberFunction, false) as NumberObject;
					if (args.Length > 0) num.Number = Convert.ToDouble(args[0]);
					return num;
				}, (proto) =>
				{
					proto["toString"] = new NativeFunctionObject("toString", (ctx, owner, args) =>
					{
						string result = string.Empty;

						if (owner != null && args.Length > 0)
						{
							double num = ((NumberObject)owner).Number;

							try
							{
								return new StringObject(Convert.ToString(Convert.ToInt32(num), Convert.ToInt32(args[0])));
							}
							catch { }
						}

						return ctx.CreateNewObject(StringFunction, new object[] { result });
					});
				});

			DateFunction = new TypedNativeFunctionObject
				(typeof(DateObject), "Date", (ctx, owner, args) =>
				{
					DateObject dateObj = owner as DateObject;
					if (dateObj == null) dateObj = ctx.CreateNewObject(DateFunction, false) as DateObject;
					return dateObj;
				});

			ArrayFunction = new ArrayConstructorFunction();

		}

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
				MessageBox.Show(Convert.ToString(args[0]));
			}
			return null;
		});

		private static readonly NativeFunctionObject __eval__ = new NativeFunctionObject("eval", (ctx, owner, args) =>
		{
			if (args.Length == 0) return false;
			return ctx.Srm.CalcExpression(Convert.ToString(args[0]));
		});

		public void ApplyToScriptRunningMachine(ScriptRunningMachine srm)
		{
			if (srm != null && srm.GlobalObject != null)
			{
				// built-in object constructors
				srm.SetGlobalVariable(ObjectFunction.Name, ObjectFunction);
				srm.SetGlobalVariable(FunctionFunction.Name, FunctionFunction);
				srm.SetGlobalVariable(StringFunction.Name, StringFunction);
				srm.SetGlobalVariable(NumberFunction.Name, NumberFunction);
				srm.SetGlobalVariable(DateFunction.Name, DateFunction);
				srm.SetGlobalVariable(ArrayFunction.Name, ArrayFunction);

				// built-in objects
				srm.SetGlobalVariable("Math", new MathObject());

				if ((srm.CoreFeatures & CoreFeatures.Console) == CoreFeatures.Console)
					srm.GlobalObject["console"] = new ObjectValue();

				if ((srm.CoreFeatures & CoreFeatures.Eval) == CoreFeatures.Eval)
					srm.GlobalObject[__eval__.Name] = __eval__;

				if ((srm.CoreFeatures & CoreFeatures.SetTimeout) == CoreFeatures.SetTimeout)
				{
					srm.GlobalObject[__setTimeout__.Name] = __setTimeout__;
					srm.GlobalObject[__clearTimeout__.Name] = __clearTimeout__;
				}

				if ((srm.CoreFeatures & CoreFeatures.SetInterval) == CoreFeatures.SetInterval)
				{
					srm.GlobalObject[__setInterval__.Name] = __setInterval__;
					srm.GlobalObject["clearInterval"] = __clearTimeout__;
				}

				if ((srm.CoreFeatures & CoreFeatures.Alert) == CoreFeatures.Alert)
					srm.GlobalObject[__alert__.Name] = __alert__;
			}
		}
	}
	#endregion

	#region WorkMode & CoreLibrary
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

	/// <summary>
	/// Specifies what features can be supported by ScriptRunningMachine.
	/// </summary>
	public enum CoreFeatures
	{
		/// <summary>
		/// A set of standard features will be supported. (Compatible with ECMAScript)
		/// Contains the alert, eval, setTimeout, setInterval functions and console object.
		/// </summary>
		StandardFeatures = Alert | Console | Eval | SetTimeout | SetInterval,

		/// <summary>
		/// Extended Feature supported by ReoScript (Non-compatible with ECMAScript)
		/// </summary>
		ExtendedFeatures = ArrayExtension,

		/// <summary>
		/// Only the built-in types will be supported
		/// </summary>
		None = 0x0,

		/// <summary>
		/// alert function support
		/// </summary>
		Alert = 0x1,

		/// <summary>
		/// eval function support
		/// </summary>
		Eval = 0x2,

		/// <summary>
		/// setTimeout function support
		/// </summary>
		SetTimeout = 0x4,

		/// <summary>
		/// setInterval function support
		/// </summary>
		SetInterval = 0x8,

		/// <summary>
		/// console object support
		/// </summary>
		Console = 0x10,

		/// <summary>
		/// Array extension feature support
		/// </summary>
		ArrayExtension = 0x20,
	}

	#endregion

	#region ReplacedCommonTree
	/// <summary>
	/// ReplacedCommonTree is used to replace a node of syntax tree in runtime.
	/// </summary>
	class ReplacedCommonTree : CommonTree
	{
		public object Object { get; set; }

		public ReplacedCommonTree(object obj)
			: base(new CommonToken(ReoScriptLexer.REPLACED_TREE))
		{
			this.Object = obj;
		}
	}
	#endregion

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
			Console.WriteLine(text);
		}

		public void WriteLine(string line)
		{
			Console.WriteLine(line);
		}
	}
	#endregion

	#region Debug Monitor

	internal class ReoScriptObjectEventArgs : EventArgs
	{
		public object Object { get; set; }

		public AbstractFunctionObject Constructor { get; set; }

		public ReoScriptObjectEventArgs(object obj, AbstractFunctionObject constructor)
		{
			this.Object = obj;
			this.Constructor = constructor;
		}
	}

	/// <summary>
	/// Provides debug ability for ScriptRunningMachine 
	/// </summary>
	public class ScriptDebugger
	{
		public static readonly string DEBUG_OBJECT_NAME = "debug";

		public ObjectValue DebugObject { get; set; }

		public ScriptRunningMachine Srm { get; set; }

		public ScriptContext Context { get; set; }

		private int totalObjectCreated = 0;

		public int TotalObjectCreated
		{
			get { return totalObjectCreated; }
			set { totalObjectCreated = value; }
		}

		public ScriptDebugger(ScriptRunningMachine srm)
		{
			this.Srm = srm;
			this.Context = new ScriptContext(srm, ScriptRunningMachine.entryFunction);

			srm.NewObjectCreated += new EventHandler<ReoScriptObjectEventArgs>(srm_NewObjectCreated);
			srm.Resetted += (s, e) =>
			{
				if (DebugObject != null)
				{
					srm[DEBUG_OBJECT_NAME] = DebugObject;
				}
			};

			DebugObject = srm.CreateNewObject(Context, srm.BuiltinConstructors.ObjectFunction) as ObjectValue;

			if (DebugObject != null)
			{
				DebugObject["assert"] = new NativeFunctionObject("assert", (ctx, owner, args) =>
				{
					if (args.Length > 0 && (args[0] as bool?) != true)
					{
						throw new ReoScriptAssertionException(args.Length > 1 ? Convert.ToString(args[1]) : string.Empty);
					}
					return null;
				});

				DebugObject["total_created_objects"] = new ExternalProperty(
					() => { return totalObjectCreated; }, null);

				srm[DEBUG_OBJECT_NAME] = DebugObject;
			}
		}

		void srm_NewObjectCreated(object sender, ReoScriptObjectEventArgs e)
		{
			totalObjectCreated++;

			//if (DebugObject != null)
			//{
			//  // FIXME: causes StackOverflowException
			//  Srm.InvokeFunctionIfExisted(DebugObject, "onObjectCreated", e.Object);
			//}
		}
	}

	#endregion
}
