using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using unvell.ReoScript.Reflection;

namespace unvell.ReoScript.Core.Statement
{
	internal class FunctionDefineNode : CommonTree
	{
		public FunctionInfo FunctionInfo { get; set; }

		public FunctionDefineNode()
			: base(new CommonToken(ReoScriptLexer.FUNCTION_DEFINE))
		{
		}
	}
}
