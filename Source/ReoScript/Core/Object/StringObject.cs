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
using System.Collections;

using unvell.ReoScript.Core;

namespace unvell.ReoScript
{
	public class StringObject : ObjectValue
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
			this["length"] = new ExternalProperty(() => String.Length, v => { });
		}
		public override bool Equals(object obj)
		{
			return (String == null && obj == null) ? false : String.Equals(Convert.ToString(obj));
		}
		public static bool operator ==(StringObject str1, object str2)
		{
			if (!(str2 is StringObject)) return false;

			return str1.Equals((StringObject)str2);
		}
		public static bool operator !=(StringObject str1, object str2)
		{
			if (!(str2 is StringObject)) return false;

			return !str1.Equals((StringObject)str2);
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
		public override IEnumerator GetEnumerator()
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
			: base(typeof(StringObject), "String")
		{
		}

		public override object Invoke(ScriptContext context, object owner, object[] args)
		{
			return args == null || args.Length <= 0 ? string.Empty : Convert.ToString(args[0]);
		}

		public override object CreateObject(ScriptContext context, object[] args)
		{
			return new StringObject((args == null || args.Length <= 0 ? string.Empty : ScriptRunningMachine.ConvertToString(args[0])));
		}

		public override object CreatePrototype(ScriptContext context)
		{
			ScriptRunningMachine srm = context.Srm;
			ObjectValue obj = context.CreateNewObject(srm.BuiltinConstructors.ObjectFunction) as ObjectValue;

			if (obj != null)
			{
				obj["trim"] = new NativeFunctionObject("trim", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;
					return ((string)owner).Trim();
				});

				obj["indexOf"] = new NativeFunctionObject("indexOf", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;
					return args.Length == 0 ? -1 : Convert.ToString(owner).IndexOf(Convert.ToString(args[0]));
				});

				obj["lastIndexOf"] = new NativeFunctionObject("lastIndexOf", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;
					return args.Length == 0 ? -1 : Convert.ToString(owner).LastIndexOf(Convert.ToString(args[0]));
				});

				obj["charAt"] = new NativeFunctionObject("charAt", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;

					string res = string.Empty;

					if (args.Length > 0)
					{
						int index = ScriptRunningMachine.GetIntParam(args, 0, -1);
						string str = Convert.ToString(owner);

						if (index >= 0 && index < str.Length)
							res = Convert.ToString(str[index]);
					}

					return res;
				});

				obj["charCodeAt"] = new NativeFunctionObject("charCodeAt", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;

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
					if (!(owner is string || owner is StringObject)) return null;
					return args.Length == 0 ? false : Convert.ToString(owner).StartsWith(Convert.ToString(args[0]));
				});

				obj["endsWith"] = new NativeFunctionObject("endWith", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;
					return args.Length == 0 ? false : Convert.ToString(owner).EndsWith(Convert.ToString(args[0]));
				});

				obj["repeat"] = new NativeFunctionObject("repeat", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;

					int count = ScriptRunningMachine.GetIntParam(args, 0, 0);

					string result = string.Empty;

					if (count > 0)
					{
						string str = ((string)owner);
						StringBuilder sb = new StringBuilder();
						for (int i = 0; i < count; i++) sb.Append(str);
						result = sb.ToString();
					}

					return result;
				});

				//obj["join"] = new NativeFunctionObject("join", (ctx, owner, args) =>
				//{
				//  if (!(owner is string || owner is StringObject)) return null;
				//  //TODO
				//  return string.Empty;
				//});

				obj["split"] = new NativeFunctionObject("split", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;

					string str = Convert.ToString(owner);

					ArrayObject arr = ctx.CreateNewArray();
					if (args.Length == 0)
					{
						arr.List.Add(str);
					}
					else
					{
						string separator = args[0] == null ? string.Empty : Convert.ToString(args[0]);
						if (!string.IsNullOrEmpty(separator))
						{
							if (args.Length == 1)
							{
								arr.List.AddRange(str.Split(new string[] { separator },
									StringSplitOptions.RemoveEmptyEntries));
							}
							else
							{
								int limits = ScriptRunningMachine.GetIntParam(args, 1, 0);

								string[] splitted = str.Split(new string[] { separator }, limits + 1,
									StringSplitOptions.RemoveEmptyEntries);

								arr.List.Capacity = Math.Min(limits, splitted.Length);

								for (int i = 0; i < arr.List.Capacity; i++)
								{
									arr.List.Add(splitted[i]);
								}
							}
						}
					}

					return arr;
				});

				obj["substr"] = new NativeFunctionObject("substr", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;

					string str = Convert.ToString(owner);
					string newstr = string.Empty;

					if (args.Length < 1)
						return newstr;
					else
					{
						int from = ScriptRunningMachine.GetIntParam(args, 0, 0);
						if (from < 0 || from > str.Length - 1)
						{
							return newstr;
						}

						int len = ScriptRunningMachine.GetIntParam(args, 1, str.Length - from);

						newstr = str.Substring(from, len);
					}

					return newstr;
				});

				obj["toLowerCase"] = new NativeFunctionObject("toLowerCase", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;
					return Convert.ToString(owner).ToLower();
				});

				obj["toUpperCase"] = new NativeFunctionObject("toUpperCase", (ctx, owner, args) =>
				{
					if (!(owner is string || owner is StringObject)) return null;
					return Convert.ToString(owner).ToUpper();
				});

				obj["valueOf"] = new NativeFunctionObject("valueOf", (ctx, owner, args) =>
				{
					if (owner is string)
						return owner;
					else if (owner is StringObject)
						return ((StringObject)owner).String;
					else
						return null;
				});
			}

			return obj;
		}
	}
}
