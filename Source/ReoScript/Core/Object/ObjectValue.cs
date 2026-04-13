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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using unvell.ReoScript.Core.Statement;

namespace unvell.ReoScript
{
	class BreakNode : ISyntaxTreeReturn
	{
		public static readonly BreakNode Value = new BreakNode();
		private BreakNode() { }
	}
	class ContinueNode : ISyntaxTreeReturn
	{
		public static readonly ContinueNode Value = new ContinueNode();
		private ContinueNode() { }
	}


	/// <summary>
	/// Object instance of ReoScript
	/// </summary>
	public class ObjectValue : ISyntaxTreeReturn, IEnumerable, IVariableContainer
	{
		/// <summary>
		/// Construct an object instance
		/// </summary>
		public ObjectValue()
		{
			Members = new Dictionary<string, object>();
		}

		private Dictionary<string, object> Members { get; set; }

		/// <summary>
		/// Get or set property
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public virtual object this[string identifier]
		{
			get
			{
				object v;
				return Members.TryGetValue(identifier, out v) ? v : null;
			}
			set
			{
				Members[identifier] = value;
			}
		}

		/// <summary>
		/// Check whether a property exists in object
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public bool HasOwnProperty(string identifier)
		{
			return Members.ContainsKey(identifier);
		}

		public object GetOwnProperty(string identifier)
		{
			return this[identifier];
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

		public virtual string Name { get { return Constructor == null ? "Object" : Constructor.FunName; } }

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
				if (this is ArrayObject || this is StringObject || this is NumberObject || this is BooleanObject)
				{
					sb.AppendLine(string.Format("[object {0}: {1}]", Name, this.ToString()));
				}
				else
				{
					sb.AppendLine(string.Format("[object {0}]", Name));
				}

				foreach (string name in Members.Keys)
				{
					if (name == ScriptRunningMachine.KEY___PROTO__) continue;

					object val = Members[name];

					sb.AppendLine(string.Format("  {0,-20}: {1}", name, (val == null ? "null" : Convert.ToString(val))));
				}

				return sb.ToString();
			}
			else
				return Name;
		}

		#region IEnumerable Members

		public virtual IEnumerator GetEnumerator()
		{
			string[] properties = Members.Keys.ToArray<string>();

			//FIXME: manage all of internal property names by SRM
			for (int i = 0; i < properties.Length; i++)
			{
				string key = properties[i];

				if (key != ScriptRunningMachine.KEY___PROTO__
						&& key != ScriptRunningMachine.KEY___ARGS__
						/*&& key != ScriptRunningMachine.KEY_CONSTRUCTOR*/)
				{
					yield return key;
				}
			}
		}

		#endregion

		internal AbstractFunctionObject Constructor { get; set; }

		/// <summary>
		/// Add properties given by IDictionary
		/// </summary>
		/// <param name="properties"></param>
		public void AddProperties(IDictionary<string, object> properties)
		{
			foreach (string key in properties.Keys)
			{
				Members[key] = properties[key];
			}
		}

		public bool TryGetValue(string identifier, out object value)
		{
			return Members.TryGetValue(identifier, out value);
		}
	}

	class ObjectConstructorFunction : TypedNativeFunctionObject
	{
		private ObjectValue rootPrototype = new ObjectValue();

		public ObjectConstructorFunction()
			: base("Object")
		{
			// check whether property existed in owner object
			rootPrototype["hasOwnProperty"] = new NativeFunctionObject("hasOwnProperty", (ctx, owner, args) =>
			{
				if (args.Length < 1)
					return false;

				if (owner is string)
				{
					if (Convert.ToString(args[0]) == "length")
						return true;
				}

				ObjectValue ownerObject = owner as ObjectValue;

				if (ownerObject == null)
					return false;

				return ownerObject.HasOwnProperty(Convert.ToString(args[0]));
			});

			// remove only own property from owner object
			rootPrototype["removeOwnProperty"] = new NativeFunctionObject("removeOwnProperty", (ctx, owner, args) =>
			{
				ObjectValue ownerObject = owner as ObjectValue;

				if (ownerObject == null || args.Length < 1)
					return false;

				return ownerObject.RemoveOwnProperty(Convert.ToString(args[0]));
			});

			rootPrototype["toString"] = new NativeFunctionObject("toString", (ctx, owner, args) =>
			{
				return owner.ToString();
			});

			rootPrototype["valueOf"] = new NativeFunctionObject("valueOf", (ctx, owner, args) =>
			{
				return owner;
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
}
