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

using unvell.ReoScript.Reflection;

namespace unvell.ReoScript
{
	internal class VariableDefineNode : SyntaxNode
	{
		public VariableInfo VariableInfo { get; set; }

		public VariableDefineNode()
			: base(NodeType.LOCAL_DECLARE_ASSIGNMENT)
		{
		}
	}
}
