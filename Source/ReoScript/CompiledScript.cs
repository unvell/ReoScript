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

using System.Collections;
using System.Collections.Generic;

using unvell.ReoScript.Core.Statement;
using unvell.ReoScript.Reflection;

namespace unvell.ReoScript
{
	/// <summary>
	/// Compiled script instance in memory. Script in text will be pre-interpreted and converted into a syntax-tree.
	/// </summary>
	public class CompiledScript
	{
		internal SyntaxNode RootNode { get; set; }

		/// <summary>
		/// Errors happened at compiling-time
		/// </summary>
		public List<ErrorObject> CompilingErrors { get; set; }

		internal StaticFunctionScope RootScope { get; set; }

		internal CompiledScript()
		{
		}

		/// <summary>
		/// Get all global functions defined in global object.
		/// </summary>
		public List<FunctionInfo> DeclaredFunctions
		{
			get
			{
				return RootScope.Functions;
			}
		}

		/// <summary>
		/// Get all local variables defined in global object.
		/// </summary>
		public List<VariableInfo> DeclaredVariables
		{
			get
			{
				return RootScope.Variables;
			}
		}

		internal static IEnumerable IterateAST(SyntaxNode node)
		{
			if (node != null && node.ChildCount > 0)
			{
				foreach (SyntaxNode t in node.Children)
				{
					if (t.ChildCount > 0)
						yield return IterateAST(node);
					else
						yield return t;
				}
			}
		}
	}
}
