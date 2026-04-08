using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace unvell.ReoScript.Core
{
	sealed internal partial class ReoScriptLexer
	{
		public static readonly int HIDDEN = Hidden;

		public const int MAX_TOKENS = 200;
		public const int REPLACED_TREE = MAX_TOKENS - 1;
	}
}
