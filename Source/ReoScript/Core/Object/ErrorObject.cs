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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace unvell.ReoScript
{
	#region ErrorObject

	/// <summary>
	/// Error object describes an error happening in script execution.
	/// </summary>
	public class ErrorObject : ObjectValue
	{
		/// <summary>
		/// Execution context when error happened
		/// </summary>
		public ScriptContext Context { get; set; }

		/// <summary>
		/// Message of error content
		/// </summary>
		public string Message { get; set; }

		internal List<CallScopeObject> CallStack { get; set; }

		/// <summary>
		/// Character index of line where error happening
		/// </summary>
		public int CharIndex { get; set; }

		/// <summary>
		/// Line number where error happening
		/// </summary>
		public int Line { get; set; }

		/// <summary>
		/// Source file path where error happened (null for inline scripts)
		/// </summary>
		public string FilePath { get; set; }

		/// <summary>
		/// Construct an error without message content
		/// </summary>
		public ErrorObject() : this(string.Empty) { }

		/// <summary>
		/// Construct an error with a message content
		/// </summary>
		/// <param name="msg"></param>
		public ErrorObject(string msg)
		{
			this.Message = msg;

			this["message"] = new ExternalProperty(() => Message);
			this["charIndex"] = new ExternalProperty(() => CharIndex);
			this["line"] = new ExternalProperty(() => Line);
			this["file"] = new ExternalProperty(() => FilePath);
			this["stack"] = new ExternalProperty(() => CallStack);
		}

		/// <summary>
		/// Get entire information of error contains call-stack.
		/// </summary>
		/// <returns></returns>
		public string GetFullErrorInfo()
		{
			StringBuilder sb = new StringBuilder();

			// Include file and line info in the error header
			if (!string.IsNullOrEmpty(FilePath))
			{
				sb.Append(Path.GetFileName(FilePath));
				if (Line > 0)
				{
					sb.AppendFormat(":{0}", Line);
					if (CharIndex > 0) sb.AppendFormat(":{0}", CharIndex);
				}
				sb.Append(" - ");
			}
			else if (Line > 0)
			{
				sb.AppendFormat("line {0}", Line);
				if (CharIndex > 0) sb.AppendFormat(":{0}", CharIndex);
				sb.Append(" - ");
			}

			sb.Append(Message);

			if (CallStack != null && CallStack.Count > 0)
			{
				sb.AppendLine();
				sb.Append(DumpCallStack());
			}

			return sb.ToString();
		}

		/// <summary>
		/// Get call-stack inforamtion about error
		/// </summary>
		/// <returns></returns>
		public string DumpCallStack()
		{
			if (CallStack == null) return string.Empty;

			StringBuilder sb = new StringBuilder();

			foreach (CallScopeObject scope in CallStack)
			{
				sb.AppendLine("    " + scope.ToString());
			}

			return sb.ToString();
		}

		/// <summary>
		/// Customized error object can be thrown from script
		/// </summary>
		public object CustomeErrorObject { get; set; }
	}

	internal class CallScopeObject : ObjectValue
	{
		internal CallScope CallScope { get; set; }

		internal CallScopeObject() { }

		internal CallScopeObject(CallScope callScope)
		{
			this.CallScope = callScope;
		}

		public override string ToString()
		{
			return CallScope.ToString();
		}
	}

	internal class ErrorConstructorFunction : TypedNativeFunctionObject
	{
		public ErrorConstructorFunction() : base(typeof(ErrorObject), "Error") { }

		public override object CreateObject(ScriptContext context, object[] args)
		{
			return (args == null || args.Length <= 0 ? new ErrorObject() : new ErrorObject(ScriptRunningMachine.ConvertToString(args[0])));
		}

		public override object CreatePrototype(ScriptContext context)
		{
			object obj = base.CreatePrototype(context);
			ObjectValue proto = obj as ObjectValue;
			if (proto != null)
			{
				proto["dumpStack"] = new NativeFunctionObject("dumpStack", (ctx, owner, args) =>
				{
					if (!(owner is ErrorObject)) return string.Empty;
					else return ((ErrorObject)owner).GetFullErrorInfo();
				});
			}
			return obj;
		}
	}
	#endregion
}
