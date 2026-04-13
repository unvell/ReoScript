/*****************************************************************************
 *
 * ReoScript - .NET Script Language Engine
 *
 * https://github.com/jingwood/ReoScript
 *
 * MIT License
 * Copyright 2012-2019 Jingwood
 *
 *****************************************************************************/

namespace unvell.ReoScript
{
	/// <summary>
	/// Token types produced by the lexer. These are a subset of NodeType —
	/// only the values that actually appear in source text.
	/// We reuse the same int constants from NodeType so that the parser can
	/// create AST nodes directly from token types.
	/// </summary>
	struct Token
	{
		public int Type;
		public string Text;
		public int Line;
		public int CharPosition;

		/// <summary>
		/// True when at least one newline (line terminator) appeared in the
		/// source text between the previous token and this token.
		/// Used by the parser for Automatic Semicolon Insertion (ASI).
		/// </summary>
		public bool NewlineBefore;

		public Token(int type, string text, int line, int charPos)
		{
			Type = type;
			Text = text;
			Line = line;
			CharPosition = charPos;
			NewlineBefore = false;
		}

		public override string ToString()
		{
			return $"[{Type}] '{Text}' ({Line}:{CharPosition})";
		}
	}
}
