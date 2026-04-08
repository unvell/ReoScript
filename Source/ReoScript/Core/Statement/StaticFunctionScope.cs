using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using unvell.ReoScript.Reflection;

namespace unvell.ReoScript.Core.Statement
{
	public class StaticFunctionScope
	{
		internal readonly List<FunctionInfo> Functions;
		internal readonly List<VariableInfo> Variables;

		internal protected StaticFunctionScope()
		{
			Functions = new List<FunctionInfo>();
			Variables = new List<VariableInfo>();
		}
	}
}
