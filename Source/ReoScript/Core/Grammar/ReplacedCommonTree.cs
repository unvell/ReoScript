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

using Antlr.Runtime;
using Antlr.Runtime.Tree;
using unvell.ReoScript.Core;

namespace unvell.ReoScript
{
	/// <summary>
	/// ReplacedCommonTree is used to replace a node of syntax tree in runtime.
	/// </summary>
	class ReplacedCommonTree : CommonTree
	{
		public object Object { get; set; }

		public ReplacedCommonTree(object obj)
			: base(new CommonToken(ReoScriptLexer.REPLACED_TREE))
		{
			this.Object = obj;
		}
	}

	class ConstValueNode : CommonTree
	{
		public object ConstValue { get; set; }

		public ConstValueNode(CommonTree t, object constValue, int tokenType)
			: base(t)
		{
			this.ConstValue = constValue;
			this.Token = new CommonToken(ReoScriptLexer.CONST_VALUE);
			this.TokenType = tokenType;
		}

		public int TokenType { get; set; }

		//public override int Type
		//{
		//  get { return ReoScriptLexer.CONST_VALUE; }
		//  set { base.Type = value; }
		//}

		//public override bool IsNil
		//{
		//  get { return false; }
		//}
	}
}
