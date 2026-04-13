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
using System.Text;

namespace unvell.ReoScript
{
	#region Extension Attributes
	[AttributeUsage(AttributeTargets.Field |
		 AttributeTargets.Property | AttributeTargets.Method)]
	public class RSPropertyAttribute : Attribute
	{
	}
	#endregion

	#region Exceptions
	/// <summary>
	/// Base exception of all exceptions
	/// </summary>
	public class ReoScriptException : Exception
	{
		public ErrorObject ErrorObject { get; set; }
		public ReoScriptException(string msg) : base(msg) { this.ErrorObject = new ErrorObject() { Message = msg }; }
		public ReoScriptException(ErrorObject error) : this(error == null ? string.Empty : error.Message) { this.ErrorObject = error; }
		public ReoScriptException(ErrorObject error, Exception inner) : base(inner == null ? string.Empty : inner.Message) { this.ErrorObject = error; }
		public ReoScriptException(string msg, Exception inner) : base(msg, inner) { }

		public string GetFullErrorInfo()
		{
			ErrorObject e = ErrorObject as ErrorObject;
			if (e == null)
			{
				return this.Message;
			}
			else
			{
				return e.GetFullErrorInfo();
			}
		}
	}
	/// <summary>
	/// Runtime error exception
	/// </summary>
	public class ReoScriptRuntimeException : ReoScriptException
	{
		public ReoScriptRuntimeException(string msg) : base(msg) { }
		public ReoScriptRuntimeException(ErrorObject error) : base(error) { }
		public ReoScriptRuntimeException(ErrorObject error, Exception inner) : base(error, inner) { }
	}
	/// <summary>
	/// Compile-time error exception
	/// </summary>
	public class ReoScriptCompilingException : ReoScriptException
	{
		public ReoScriptCompilingException(ErrorObject error) : base(error) { }
	}
	//public class ReoScriptSyntaxErrorException : ReoScriptCompilingException
	//{
	//  public ReoScriptSyntaxErrorException(ErrorObject error) : base(error) { }
	//}
	public enum SyntaxErrorType
	{
		ExpectToken,
		UnexpectedToken,
		MissingToken,
		MistakeToken,
	}

	/// <summary>
	/// Assertion failure exception. This exception thrown by debug.assert built-in function.
	/// </summary>
	public class ReoScriptAssertionException : ReoScriptRuntimeException
	{
		public ReoScriptAssertionException(string msg) : base(msg) { }
	}

	/// <summary>
	/// Call-stack overflow exception. This exception will be thrown if the call-stack reached the max limitation.
	/// </summary>
	public class CallStackOverflowException : ReoScriptRuntimeException
	{
		public CallStackOverflowException(string msg) : base(msg) { }
	}

	/// <summary>
	/// Exception thrown when a loop exceeds the maximum allowed iterations.
	/// </summary>
	public class ScriptExecutionTimeoutException : ReoScriptRuntimeException
	{
		public ScriptExecutionTimeoutException(ErrorObject error) : base(error) { }
		public ScriptExecutionTimeoutException(string msg) : base(msg) { }
	}

	/// <summary>
	/// This exception will be thrown if script attempts to call an undefined function.
	/// </summary>
	public class FunctionNotDefinedException : ReoScriptRuntimeException
	{
		public FunctionNotDefinedException(ErrorObject error) : base(error) { }
	}

	//public class ClassNotFoundException : ReoScriptRuntimeException
	//{
	//  public ClassNotFoundException(ScriptContext context, string msg) : base(context, msg) { }
	//  public ClassNotFoundException(ScriptContext context, string msg, Exception inner) : base(context, msg, null, inner) { }
	//}

	#endregion
}
