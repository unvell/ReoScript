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

using System.Collections.Generic;
using unvell.ReoScript.Core.Statement;

namespace unvell.ReoScript
{

	#region Lexer & Parser

	namespace Reflection
	{
		/// <summary>
		/// ReoScript function information
		/// </summary>
		public class FunctionInfo
		{
			/// <summary>
			/// Function name
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// Argument name list
			/// </summary>
			public string[] Args { get; set; }

			/// <summary>
			/// Specifies whether is anonymous function.
			/// </summary>
			public bool IsAnonymous { get; set; }

			/// <summary>
			/// Specifies whether is nested function inside another function.
			/// </summary>
			public bool IsInner { get; set; }

			/// <summary>
			/// Specifies the modifier of function visibility scope.
			/// </summary>
			public MemberScopeModifier ScopeModifier { get; set; }

			/// <summary>
			/// Specifies the char position on line where function is defined.
			/// </summary>
			public int CharIndex { get; set; }

			/// <summary>
			/// Specifies the line number where function is defined.
			/// </summary>
			public int Line { get; set; }

			internal SyntaxNode BodyTree { get; set; }
			internal StaticFunctionScope InnerScope { get; set; }
			internal StaticFunctionScope OuterScope { get; set; }

			/// <summary>
			/// Get string of function's body.
			/// </summary>
			/// <returns></returns>
			public string GetBodyText()
			{
				return BodyTree?.ToString();
			}

			/// <summary>
			/// Get all inner functions defined in this function
			/// </summary>
			public List<FunctionInfo> DeclaredInnerFunctions
			{
				get
				{
					return InnerScope?.Functions;
				}
			}

			/// <summary>
			/// Get local variables defined in this function
			/// </summary>
			public List<VariableInfo> DeclaredLocalVariables
			{
				get
				{
					return InnerScope?.Variables;
				}
			}
		}
	}

	#endregion
}
