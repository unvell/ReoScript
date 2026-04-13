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

namespace unvell.ReoScript
{

	namespace Reflection
	{
		/// <summary>
		/// ReoScript variable information
		/// </summary>
		public class VariableInfo
		{
			/// <summary>
			/// Variable name
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// Specifies whether is local variable.
			/// </summary>
			public bool IsLocal { get; set; }

			/// <summary>
			/// Specifies whether the variable to be used without declaration.
			/// </summary>
			public bool IsImplicitDeclaration { get; set; }

			/// <summary>
			/// Specifies whether the variable has an initialize value.
			/// </summary>
			public bool HasInitialValue { get; set; }

			/// <summary>
			/// Specifies the modifier of variable visibility scope.
			/// </summary>
			public MemberScopeModifier ScopeModifier { get; set; }

			/// <summary>
			/// Specifies the char position on line where variable is defined.
			/// </summary>
			public int CharIndex { get; set; }

			/// <summary>
			/// Specifies the line number where variable is defined.
			/// </summary>
			public int Line { get; set; }

			internal SyntaxNode InitialValueTree { get; set; }

			/// <summary>
			/// Get string of initial value expression.
			/// </summary>
			/// <returns></returns>
			public string GetInitialValueExpression()
			{
				return InitialValueTree?.ToString();
			}
		}
	}

}
