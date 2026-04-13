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

namespace unvell.ReoScript
{
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
	/// Event arguments for script errors caught during event handler execution.
	/// </summary>
	public class ScriptErrorEventArgs : EventArgs
	{
		/// <summary>
		/// The exception that occurred.
		/// </summary>
		public ReoScriptException Exception { get; }

		public ScriptErrorEventArgs(ReoScriptException exception)
		{
			this.Exception = exception;
		}
	}
}
