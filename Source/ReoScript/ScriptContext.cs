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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using unvell.ReoScript.Parsers;
using unvell.ReoScript.Core.Statement;

namespace unvell.ReoScript
{
	/// <summary>
	/// A script context used in multi-thread executing
	/// </summary>
	public sealed class ScriptContext
	{
		internal static readonly int MAX_STACK = 50;

		#region Context Constructor

		/// <summary>
		/// Path of file loaded by Load method of SRM
		/// </summary>
		internal ScriptContext(ScriptRunningMachine srm, AbstractFunctionObject function) :
			this(srm, function, null)
		{ }

		internal ScriptContext(ScriptRunningMachine srm, AbstractFunctionObject function, string filePath)
		{
#if DEBUG
			Debug.Assert(srm != null);
			Debug.Assert(srm.GlobalObject != null);
			Debug.Assert(function != null);
#endif

			this.SourceFilePath = filePath;
			this.GlobalObject = srm.GlobalObject;
			this.Srm = srm;

#if EXTERNAL_GETTER_SETTER
			PropertyGetter=new Dictionary<Func<string,bool>,Func<string,object>>();
			PropertySetter=new Dictionary<Func<string,bool>,Func<string,object>>();
#endif

			FunctionStack = new Stack<CallScope>();
			callStack.Push(new CallScope(this.GlobalObject, ScriptRunningMachine.entryFunction));

		}

		public string SourceFilePath { get; set; }

		/// <summary>
		/// Current context this object
		/// </summary>
		public object ThisObject
		{
			get
			{
				return CurrentCallScope == null ? GlobalObject : CurrentCallScope.ThisObject;
			}
			set
			{
				if (CurrentCallScope != null) CurrentCallScope.ThisObject = value;
			}
		}

		#endregion

		#region Variable & Property

		/// <summary>
		/// ScriptRunningMachine instance
		/// </summary>
		public ScriptRunningMachine Srm { get; set; }

		/// <summary>
		/// Global object (Root object in script context)
		/// </summary>
		internal ObjectValue GlobalObject { get; set; }

		/// <summary>
		/// Not supported
		/// </summary>
		internal ObjectValue WithObject { get; set; }

		/// <summary>
		/// Get or set variable in current call-stack.
		/// </summary>
		/// <param name="identifier">name of variable</param>
		/// <returns>value of variable</returns>
		public object this[string identifier]
		{
			get
			{
#if EXTERNAL_GETTER_SETTER
				foreach (var getter in PropertyGetter)
				{
					if (getter.Key(identifier))
					{
						return getter.Value(identifier);
					}
				}
#endif

				IVariableContainer container = null;

				CallScope cs = CurrentCallScope;

				if (cs != null)
				{
					if (cs.Variables.ContainsKey(identifier))
					{
						container = cs;
					}
					else
					{
						CallScope outerScope = cs.CurrentFunction.CapturedScope;
						while (outerScope != null)
						{
							if (outerScope.Variables.ContainsKey(identifier))
							{
								container = outerScope;
								break;
							}

							outerScope = outerScope.CurrentFunction.CapturedScope;
						}
					}

					// If not found via CapturedScope chain, search up the call stack
					// but ONLY when inside a template tag scope. This allows nested
					// tags inside templates to resolve template parameters without
					// leaking caller locals into normal function calls.
					if (container == null)
					{
						bool insideTemplate = false;
						foreach (var stackScope in callStack)
						{
							if (stackScope.CurrentFunction is TemplateConstructorObject)
							{
								insideTemplate = true;
								break;
							}
						}

						if (insideTemplate)
						{
							foreach (var stackScope in callStack)
							{
								if (stackScope != cs && stackScope.Variables.ContainsKey(identifier))
								{
									container = stackScope;
									break;
								}
							}
						}
					}
				}

				if (container == null)
				{
					container = GlobalObject;
				}

				object o = null;
				container.TryGetValue(identifier, out o);

				//if (CurrentCallScope == null)
				//  return GlobalObject[identifier];

				//object obj = CurrentCallScope[identifier];

				//if (obj != null)
				//  return obj;
				//else
				//  return GlobalObject[identifier];
				return o;
			}
			set
			{
#if EXTERNAL_GETTER_SETTER
				foreach (var setter in PropertySetter)
				{
					if (setter.Key(identifier)) setter.Value(identifier);
				}
#endif

				if (CurrentCallScope != null)
					CurrentCallScope[identifier] = value;
				else
					GlobalObject[identifier] = value;
			}
		}

		/// <summary>
		/// Set variable by specified name into current call-stack.
		/// If does not exist, set variable into global object.
		/// </summary>
		/// <param name="identifier">name of variable</param>
		/// <param name="value">value of variable</param>
		/// <returns>value to be set</returns>
		public object SetVariable(string identifier, object value)
		{
			if (CurrentCallScope != null)
				CurrentCallScope[identifier] = value;
			else
				GlobalObject[identifier] = value;

			return value;
		}

		/// <summary>
		/// Get variable by specified name from current call-stack.
		/// If does not exist, get variable from global object.
		/// </summary>
		/// <param name="identifier">name of variable</param>
		/// <returns>value of specified variable</returns>
		public object GetVariable(string identifier)
		{
			if (CurrentCallScope == null)
				return GlobalObject[identifier];

			object obj = CurrentCallScope[identifier];

			if (obj != null)
				return obj;
			else
				return GlobalObject[identifier];
		}

		/// <summary>
		/// Remove specified variable from current call-stack.
		/// If variable cannot be found in current call-stack, remove variable from global object.
		/// </summary>
		/// <param name="errorObjIdentifier">name of variable</param>
		public void RemoveVariable(string identifier)
		{
			if (CurrentCallScope == null)
				GlobalObject.RemoveOwnProperty(identifier);
			else
				CurrentCallScope.Variables.Remove(identifier);
		}

		/// <summary>
		/// Evaluate a property getting operation.
		/// </summary>
		/// <param name="obj">get specified property from this object</param>
		/// <param name="identifier">name of property</param>
		/// <returns>value of property</returns>
		public object EvaluatePropertyGet(ObjectValue obj, string identifier)
		{
			return PropertyAccessHelper.GetProperty(this, obj, identifier);
		}

		/// <summary>
		/// Evaluate a property setting operation.
		/// </summary>
		/// <param name="obj">set specified property into this object</param>
		/// <param name="identifier">name of property</param>
		/// <param name="value">value of property</param>
		public void EvaluatePropertySet(ObjectValue obj, string identifier, object value)
		{
			PropertyAccessHelper.SetProperty(this, obj, identifier, value);
		}

#if EXTERNAL_GETTER_SETTER
		public Dictionary<Func<string, bool>, Func<string, object>> PropertyGetter { get; set; }

		public Dictionary<Func<string, bool>, Func<string, object>> PropertySetter { get; set; }

		public Func<string, string, object> ExternalRangeGenerator { get; set; }
#endif

		#endregion

		#region CallStack
		private readonly Stack<CallScope> callStack = new Stack<CallScope>();

		/// <summary>
		/// Call-stack for function call.
		/// </summary>
		internal Stack<CallScope> CallStack { get { return callStack; } }

		internal Stack<CallScope> FunctionStack { get; set; }

		/// <summary>
		/// Current call-stack of function call.
		/// </summary>
		internal CallScope CurrentCallScope { get; set; }

		/// <summary>
		/// Push a call-scope into call-stack.
		/// </summary>
		/// <param name="scope">call-scope will be pushed into call-stack</param>
		internal void PushCallStack(CallScope scope, bool innerCall)
		{
			if (callStack.Count >= MAX_STACK)
			{
				throw new CallStackOverflowException("Call stack overflow.");
			}

#if DEBUG
			Debug.Assert(scope != null);
			Debug.Assert(scope.CurrentFunction != null);

			// allow null 'this' reference
			//Debug.Assert(scope.ThisObject != null);
#endif

			callStack.Push(scope);

			CurrentCallScope = scope;
		}

		internal void PopCallStack()
		{
			if (callStack.Count > 0) callStack.Pop();
			CurrentCallScope = callStack.Count > 1 ? callStack.Peek() : null;
		}
		#endregion

		#region Create Object
		/// <summary>
		/// Create an object instance.
		/// </summary>
		/// <returns></returns>
		public ObjectValue CreateNewObject()
		{
			return Srm.CreateNewObject(this);
		}

		/// <summary>
		/// Create an object instance and add initial properties.
		/// </summary>
		/// <param name="properties">properties will be added into created instance</param>
		/// <returns>created object instance</returns>
		public ObjectValue CreateNewObject(Dictionary<string, object> properties)
		{
			return CreateNewObject((obj) => obj.AddProperties(properties));
		}

		/// <summary>
		/// Create an object instance and perform customized initialization.
		/// </summary>
		/// <param name="initializer">given initializer will be invoked after object is created</param>
		/// <returns>created object instance</returns>
		public ObjectValue CreateNewObject(Action<ObjectValue> initializer)
		{
			ObjectValue obj = CreateNewObject();
			initializer?.Invoke(obj);
			return obj;
		}

		/// <summary>
		/// Create an object using specified function constructor
		/// </summary>
		/// <param name="funObject">function constructor</param>
		/// <param name="invokeConstructor">specifies whether allow to call constructor</param>
		/// <param name="args">arguments for calling constructor</param>
		/// <returns>created object</returns>
		public object CreateNewObject(AbstractFunctionObject funObject, bool invokeConstructor = true, object[] args = null)
		{
			return Srm.CreateNewObject(this, funObject, invokeConstructor, args);
		}

		/// <summary>
		/// Create an empty array object
		/// </summary>
		/// <returns></returns>
		public ArrayObject CreateNewArray()
		{
			return CreateNewArray(null);
		}

		/// <summary>
		/// Create an array object with initial elements
		/// </summary>
		/// <param name="initElements"></param>
		/// <returns></returns>
		public ArrayObject CreateNewArray(params object[] initElements)
		{
			ArrayObject arr = Srm.CreateNewObject(this, Srm.BuiltinConstructors.ArrayFunction) as ArrayObject;
			if (arr != null && initElements != null)
			{
				arr.List.AddRange(initElements);
			}
			return arr;
		}

		internal ErrorObject CreateErrorObject(SyntaxNode t, string msg)
		{
			ErrorObject err = CreateNewObject(Srm.BuiltinConstructors.ErrorFunction) as ErrorObject;
			err.Line = t.Line;
			err.CharIndex = t.CharPositionInLine;
			err.FilePath = this.SourceFilePath;
			err.Message = msg;
			err.CallStack = new List<CallScopeObject>();

			CallScope sc = callStack.Peek();
			sc.Line = t.Line;
			sc.CharIndex = t.CharPositionInLine;
			sc.FilePath = this.SourceFilePath;

			foreach (CallScope scope in callStack)
			{
				err.CallStack.Add(new CallScopeObject(scope));
			}

			return err;
		}

		internal ReoScriptRuntimeException CreateRuntimeError(SyntaxNode t, string msg)
		{
			return new ReoScriptRuntimeException(CreateErrorObject(t, msg));
		}

		#endregion

	}
}
