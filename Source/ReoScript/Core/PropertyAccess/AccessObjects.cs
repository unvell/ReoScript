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
using System.Diagnostics;
using System.IO;
using System.Text;

using unvell.ReoScript.Core.Statement;

namespace unvell.ReoScript
{
	#region Accessor
	// FIXME: Accessor mechanism should be removed in order to improve the execution speed
	//[Obsolete("Accessor mechanism should be removed in order to improve the execution speed.")]
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
		public string Identifier { get; set; }
		public IVariableContainer Scope { get; set; }
		public object Value { get; set; }

		public VariableAccess(ScriptRunningMachine srm, ScriptContext ctx, string identifier)
			: base(srm, ctx)
		{
			this.Identifier = identifier;

			CallScope cs = ctx.CurrentCallScope;

			if (cs != null)
			{
				if (cs.Variables.ContainsKey(identifier))
				{
					Scope = cs;
				}
				else
				{
					CallScope outerScope = cs.CurrentFunction.CapturedScope;
					while (outerScope != null)
					{
						if (outerScope.Variables.ContainsKey(identifier))
						{
							Scope = outerScope;
							break;
						}

						outerScope = outerScope.CurrentFunction.CapturedScope;
					}
				}

				// If not found via CapturedScope chain, search up the call stack.
				// This allows nested tags inside templates to resolve template parameters.
				if (Scope == null)
				{
					foreach (var stackScope in ctx.CallStack)
					{
						if (stackScope != cs && stackScope.Variables.ContainsKey(identifier))
						{
							Scope = stackScope;
							break;
						}
					}
				}
			}

			if (Scope == null)
			{
				Scope = ctx.GlobalObject;
			}

#if DEBUG
			Debug.Assert(Scope != null);
#endif

			object o = null;
			Scope.TryGetValue(Identifier, out o);
			Value = o;

		}

		//private CallScope scope;
		//public CallScope Scope { get { return scope; } }
		//public ObjectValue GlobalObject { get; set; }
		#region Access Members
		public override void Set(object value)
		{
			if (Value is ExternalProperty)
			{
				((ExternalProperty)Value).Setter(value);
			}
			else
			{
				Scope[Identifier] = value;
			}
		}
		public override object Get()
		{
			return Value;
			//object o = null;
			//Scope.TryGetValue(Identifier, out o);
			//return o;

			//if (Identifier == ScriptRunningMachine.GLOBAL_VARIABLE_NAME)
			//{
			//  return Context.GlobalObject;
			//}
			//else
			//  return Scope == null ? (GlobalObject == null ? null : GlobalObject[Identifier]) : Scope[Identifier];
		}
		#endregion Access Members
	}
	#endregion Variable Access

	#region Array Access
	class ArrayAccess : AccessValue
	{
		public IList Array { get; set; }
		public int Index { get; set; }

		public ArrayAccess(ScriptRunningMachine srm, ScriptContext ctx, IList array, int index)
			: base(srm, ctx)
		{
			this.Array = array;
			this.Index = index;
		}

		#region Access Members
		public override void Set(object value)
		{
			Array[Index] = value;
		}

		public override object Get()
		{
			return Index >= Array.Count ? null : Array[Index];
		}
		#endregion
	}
	#endregion Array Access

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
			PropertyAccessHelper.SetProperty(Context, obj, identifier, value);
		}
		public override object Get()
		{
			return PropertyAccessHelper.GetProperty(Context, obj, identifier);
		}
		#endregion
	}
	#endregion Property Access

	#region String Access
	class StringAccess : AccessValue
	{
		public object StringVariable { get; set; }
		public int Index { get; set; }

		public StringAccess(ScriptRunningMachine srm, ScriptContext ctx, object array, int index)
				: base(srm, ctx)
		{
			this.StringVariable = array;
			this.Index = index;
		}

		#region Access Members
		public override void Set(object value)
		{
			// modifying string is not supported
		}

		public override object Get()
		{
			if (StringVariable is string str)
			{
				return Context.CreateNewObject(Srm.BuiltinConstructors.StringFunction, true, new object[] { str[Index].ToString() });
			}
			else if (StringVariable is StringObject strobj)
			{
				return Context.CreateNewObject(Srm.BuiltinConstructors.StringFunction, true, new object[] { strobj.String[Index].ToString() });
			}
			else if (StringVariable is StringBuilder sb)
			{
				return Context.CreateNewObject(Srm.BuiltinConstructors.StringFunction, true, new object[] { sb[Index].ToString() });
			}
			else
			{
				return null;
			}
		}
		#endregion Access Members
	}
	#endregion String Access

	#endregion Accessor

	interface IVariableContainer
	{
		object this[string identifier] { get; set; }
		bool TryGetValue(string identifier, out object value);
	}

	internal class CallScope : IVariableContainer
	{
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
				object o;
				return (variables.TryGetValue(identifier, out o)) ? o : null;
			}
			set
			{
				variables[identifier] = value;
			}
		}

		public bool TryGetValue(string identifier, out object value)
		{
			return variables.TryGetValue(identifier, out value);
		}

		public bool IsInnerCall { get; set; }

		public int CharIndex { get; set; }
		public int Line { get; set; }

		public string FilePath { get; set; }

		public override string ToString()
		{
			string funcName = GetFunctionName(CurrentFunction);
			if (!string.IsNullOrEmpty(FilePath))
				return string.Format("at {0} ({1}:{2}:{3})", funcName, Path.GetFileName(FilePath), Line, CharIndex);
			else
				return string.Format("at {0} (line {1}:{2})", funcName, Line, CharIndex);
		}

		private static string GetFunctionName(AbstractFunctionObject fun)
		{
			if (fun is FunctionObject)
			{
				FunctionObject funObj = ((FunctionObject)fun);

				if (funObj.FunctionInfo != null && funObj.FunctionInfo.IsAnonymous)
				{
					return "<anonymous>";
				}
			}

			return fun.FunName;
		}

	}
}
