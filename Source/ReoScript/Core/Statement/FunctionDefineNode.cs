using unvell.ReoScript.Reflection;

namespace unvell.ReoScript.Core.Statement
{
	internal class FunctionDefineNode : SyntaxNode
	{
		public FunctionInfo FunctionInfo { get; set; }

		public FunctionDefineNode()
			: base(NodeType.FUNCTION_DEFINE)
		{
		}
	}
}
