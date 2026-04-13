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
using System.Text;

namespace unvell.ReoScript
{
	#region Array
	public class ArrayObject : ObjectValue, IList
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
				() => this.Length,
				v => this.Length = ScriptRunningMachine.GetIntValue(v, List.Count));
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
					object[] empty = new object[len - list.Count];
					list.AddRange(empty);
					//ArrayList newArr = new ArrayList(len+5);
					//newArr.AddRange(list);
					//list = newArr;
				}
			}
		}

		public bool IsReadOnly => false;

		public bool IsFixedSize => false;

		public int Count => this.list.Count;

		public object SyncRoot => this.list.SyncRoot;

		public bool IsSynchronized => this.list.IsSynchronized;

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
					this.Length = index + 1;
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

		public override IEnumerator GetEnumerator()
		{
			for (int i = 0; i < list.Count; i++)
			{
				yield return list[i];
			}
		}

		public int Add(object value)
		{
			return this.list.Add(value);
		}

		public bool Contains(object value)
		{
			return this.list.Contains(value);
		}

		public void Clear()
		{
			this.list.Clear();
		}

		public int IndexOf(object value)
		{
			return this.list.IndexOf(value);
		}

		public void Insert(int index, object value)
		{
			this.list.Insert(index, value);
		}

		public void Remove(object value)
		{
			this.list.Remove(value);
		}

		public void RemoveAt(int index)
		{
			this.list.RemoveAt(index);
		}

		public void CopyTo(Array array, int index)
		{
			this.list.CopyTo(array, index);
		}
		#endregion IEnumerable Members

	}
	class ArrayConstructorFunction : TypedNativeFunctionObject
	{
		public ArrayConstructorFunction() :
			base(typeof(ArrayObject), "Array")
		{ }

		public override object Invoke(ScriptContext context, object owner, object[] args)
		{
			return base.Invoke(context, owner, args);
		}

		public override object CreateObject(ScriptContext context, object[] args)
		{
			ArrayObject arr = new ArrayObject();
			if (args != null)
			{
				arr.List.AddRange(args);
			}
			return arr;
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

				objValue["slice"] = new NativeFunctionObject("slice", (ctx, owner, args) =>
				{
					if (args.Length < 1 || !(owner is ArrayObject)) return false;

					ArrayObject arr = (ArrayObject)owner;
					ArrayObject newArray = ctx.CreateNewArray();

					int index = ScriptRunningMachine.GetIntParam(args, 0, 0);
					if (index < 0 || index >= arr.Length)
					{
						return newArray;
					}

					int howmany = ScriptRunningMachine.GetIntParam(args, 1, arr.Length - index);

					newArray.List.AddRange(arr.List.GetRange(index, howmany));

					return newArray;
				});

				objValue["splice"] = new NativeFunctionObject("splice", (ctx, owner, args) =>
				{
					if (args.Length < 2 || !(owner is ArrayObject)) return false;

					ArrayObject arr = (ArrayObject)owner;

					int index = ScriptRunningMachine.GetIntParam(args, 0, 0);
					if (index < 0) index = 0;
					if (index >= arr.Length) return false;

					int howmany = ScriptRunningMachine.GetIntParam(args, 1, arr.Length - index);

					arr.List.RemoveRange(index, howmany);

					for (int i = 2; i < args.Length; i++)
						arr.List.Insert(index++, args[i]);

					return true;
				});

				objValue["remove"] = new NativeFunctionObject("remove", (ctx, owner, args) =>
				{
					if (args.Length <= 0 || !(owner is ArrayObject)) return null;

					ArrayObject arr = (ArrayObject)owner;
					arr.List.Remove(args[0]);

					return null;
				});

				objValue["indexOf"] = new NativeFunctionObject("indexOf", (ctx, owner, args) =>
				{
					if (!(owner is ArrayObject)) return NaNValue.Value;

					if (args == null || args.Length <= 0) return -1;

					return ((ArrayObject)owner).List.IndexOf(args[0]);
				});

				objValue["sort"] = new NativeFunctionObject("sort", (ctx, owner, args) =>
				{
					if (!(owner is ArrayObject)) return null;

					((ArrayObject)owner).List.Sort();
					return null;
				});

				objValue["join"] = new NativeFunctionObject("join", (ctx, owner, args) =>
				{
					if (!(owner is ArrayObject)) return null;

					string separator = args == null || args.Length == 0 ? "," : Convert.ToString(args[0]);

					StringBuilder sb = new StringBuilder();
					foreach (object element in ((ArrayObject)owner).List)
					{
						if (sb.Length > 0) sb.Append(separator);
						sb.Append(Convert.ToString(element));
					}

					return sb.ToString();
				});

				objValue["concat"] = new NativeFunctionObject("concat", (ctx, owner, args) =>
				{
					if (!(owner is ArrayObject)) return null;

					if (args.Length <= 0) return owner;

					var newArr = ctx.CreateNewArray();
					newArr.List.AddRange(((ArrayObject)owner).List);

					foreach (var arg in args)
					{
						if (arg is ArrayObject arrayArg)
						{
							newArr.List.AddRange(arrayArg.List);
						}
						else
						{
							newArr.List.Add(arg);
						}
					}

					return newArr;
				});
			}

			return obj;
		}
	}
	#endregion Array
}
