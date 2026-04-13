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
using System.Linq;
using System.Reflection;
using Antlr.Runtime.Tree;

using unvell.ReoScript.Reflection;

namespace unvell.ReoScript
{
	//public interface IFunctionObject
	//{
	//  object Invoke(ScriptRunningMachine srm, object owner, object[] args);
	//}
	//public interface IConstructorFunctionObject : IFunctionObject
	//{
	//  object CreateObject(ScriptRunningMachine srm);
	//  ObjectValue Prototype { get; set; }
	//}
	/// <summary>
	/// Abstract of ReoScript executable function
	/// </summary>
	public abstract class AbstractFunctionObject : ObjectValue
	{
		public abstract string FunName { get; set; }
		//public ObjectValue Prototype { get; set; }

		internal AbstractFunctionObject() { }

		public virtual object CreateObject(ScriptContext context, object[] args)
		{
			return new ObjectValue();
		}

		public virtual object CreatePrototype(ScriptContext context)
		{
			return context.CreateNewObject(context.Srm.BuiltinConstructors.ObjectFunction) as ObjectValue;
		}

		/// <summary>
		/// Lexical environment captured at the moment this function value was created.
		///
		/// For named/anonymous inner functions this points to the enclosing call scope
		/// at creation time, giving them proper closure semantics: the captured
		/// variables remain reachable for the entire lifetime of the function value,
		/// independent of where (or how often) the function is later called.
		///
		/// For top-level functions and native functions this is null; lookups fall
		/// through to the global object.
		///
		/// Each evaluation of a function expression / declaration produces a fresh
		/// FunctionObject instance, so two closures created from the same source
		/// have independent CapturedScope references and do not interfere.
		/// </summary>
		internal CallScope CapturedScope { get; set; }
	}

	/// <summary>
	/// Executable function defined in script
	/// </summary>
	public class FunctionObject : AbstractFunctionObject
	{
		/// <summary>
		/// Name of function
		/// </summary>
		public override string FunName { get; set; }

		/// <summary>
		/// Argument name list
		/// </summary>
		public string[] Args { get; set; }

		/// <summary>
		/// Syntax Tree of function body
		/// </summary>
		internal CommonTree Body { get; set; }

		public override string ToString()
		{
			return string.IsNullOrEmpty(FunName) ? "function() {...}"
				: "function " + FunName + "() {...}";
		}

		/// <summary>
		/// Function meta information
		/// </summary>
		public FunctionInfo FunctionInfo { get; set; }
	}

	/// <summary>
	/// Executable function defined in .NET
	/// </summary>
	public class NativeFunctionObject : AbstractFunctionObject
	{
		/// <summary>
		/// Name of function
		/// </summary>
		public override string FunName { get; set; }

		public override string ToString()
		{
			return "function " + FunName + "() { [native code] }";
		}

		/// <summary>
		/// Construct executable function with name
		/// </summary>
		/// <param name="name">name of function</param>
		public NativeFunctionObject(string name)
		{
			//this.Name = name;
			this.FunName = name;
		}

		/// <summary>
		/// Construct executable function with name and body
		/// </summary>
		/// <param name="name">name of function</param>
		/// <param name="body">body of function</param>
		public NativeFunctionObject(string name, Func<ScriptContext, object, object[], object> body)
		{
			this.FunName = name;
			this.Body = body;
		}

		/// <summary>
		/// Invoke function
		/// </summary>
		/// <param name="context">Context of script execution</param>
		/// <param name="owner">this object to call function</param>
		/// <param name="args">argument list</param>
		/// <returns></returns>
		public virtual object Invoke(ScriptContext context, object owner, object[] args)
		{
			return (Body == null ? null : Body(context, owner, args));
		}

		/// <summary>
		/// Body of function
		/// </summary>
		public Func<ScriptContext, object, object[], object> Body { get; set; }
	}

	/// <summary>
	/// Executable typed constructor function. Typed function can create an instance of object
	/// that is specified by user.
	/// </summary>
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
			//try
			//{
				object[] cargs = null;

				if (args != null && args.Length > 0)
				{
					ConstructorInfo ci = this.Type.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == args.Length);

					if (ci == null)
					{
						throw new ReoScriptRuntimeException("Cannot to create .NET instance with incorrect parameters.");
					}

					cargs = new object[args.Length];

					ParameterInfo[] pis = ci.GetParameters();
					for (int i = 0; i < args.Length; i++)
					{
						cargs[i] = context.Srm.ConvertToCLRType(context, args[i], pis[i].ParameterType);
					}
				}

				return System.Activator.CreateInstance(this.Type, BindingFlags.Default, null, cargs, null);
			//}
			//catch (Exception ex)
			//{
			//  throw new ReoScriptRuntimeException(context, "Error to create .Net instance: " + this.Type.ToString(), null, ex);
			//}
		}

		public override object CreatePrototype(ScriptContext context)
		{
			ObjectValue obj = context.CreateNewObject(context.Srm.BuiltinConstructors.ObjectFunction) as ObjectValue;

			if (obj == null) return obj;

			PrototypeBuilder?.Invoke(obj);

			return obj;
		}
	}

	/// <summary>
	/// Generic typed constructor function.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class TypedNativeFunctionObject<T> : TypedNativeFunctionObject
	{
		public TypedNativeFunctionObject()
			: this(typeof(T).Name)
		{
			// this();
		}
		public TypedNativeFunctionObject(string name)
			: this(name, null)
		{
			// this()
		}
		public TypedNativeFunctionObject(string name, Func<ScriptContext, object, object[], object> body)
			: this(name, body, null)
		{
			// this()
		}
		public TypedNativeFunctionObject(string name, Func<ScriptContext, object, object[], object> body,
			Action<ObjectValue> prototypeBuilder)
			: base(typeof(T), name, body, prototypeBuilder)
		{
			// base()
		}
	}
}
