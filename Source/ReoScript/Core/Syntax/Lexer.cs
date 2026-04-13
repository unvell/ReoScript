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

using System;
using System.Collections.Generic;

namespace unvell.ReoScript
{
	/// <summary>
	/// Hand-written lexer for ReoScript. Produces a flat list of <see cref="Token"/>
	/// values that the <see cref="ReoScriptHandwrittenParser"/> consumes.
	/// </summary>
	sealed class ReoScriptLexerNew
	{
		private readonly string source;
		private int pos;
		private int line = 1;
		private int col;

		public ReoScriptLexerNew(string source)
		{
			this.source = source ?? string.Empty;
		}

		public List<Token> Tokenize()
		{
			var tokens = new List<Token>();

			while (pos < source.Length)
			{
				SkipWhitespaceAndComments();
				if (pos >= source.Length) break;

				int startLine = line;
				int startCol = col;
				char c = source[pos];

				// String literals
				if (c == '"' || c == '\'')
				{
					tokens.Add(ReadString(c, startLine, startCol));
					continue;
				}

				// Number literals (including 0x and 0b)
				if (char.IsDigit(c) || (c == '.' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
				{
					tokens.Add(ReadNumber(startLine, startCol));
					continue;
				}

				// Identifiers and keywords
				if (IsIdentStart(c))
				{
					tokens.Add(ReadIdentifierOrKeyword(startLine, startCol));
					continue;
				}

				// Operators and punctuation
				Token? op = ReadOperator(startLine, startCol);
				if (op.HasValue)
				{
					tokens.Add(op.Value);
					continue;
				}

				// Unknown character — skip and report
				Advance();
			}

			tokens.Add(new Token(NodeType.EOF, "<EOF>", line, col));
			return tokens;
		}

		#region Whitespace & Comments

		private void SkipWhitespaceAndComments()
		{
			while (pos < source.Length)
			{
				char c = source[pos];

				if (c == ' ' || c == '\t')
				{
					Advance();
				}
				else if (c == '\r' || c == '\n')
				{
					if (c == '\r' && Peek(1) == '\n') pos++;
					pos++;
					line++;
					col = 0;
				}
				else if (c == '/' && Peek(1) == '/')
				{
					// Line comment
					while (pos < source.Length && source[pos] != '\n' && source[pos] != '\r')
						pos++;
					col = pos; // will be reset on next newline
				}
				else if (c == '/' && Peek(1) == '*')
				{
					// Block comment
					pos += 2; col += 2;
					while (pos < source.Length)
					{
						if (source[pos] == '*' && Peek(1) == '/')
						{
							pos += 2; col += 2;
							break;
						}
						if (source[pos] == '\n') { line++; col = 0; pos++; }
						else if (source[pos] == '\r')
						{
							if (Peek(1) == '\n') pos++;
							pos++; line++; col = 0;
						}
						else { pos++; col++; }
					}
				}
				else
				{
					break;
				}
			}
		}

		#endregion

		#region String

		private Token ReadString(char quote, int startLine, int startCol)
		{
			int start = pos;
			Advance(); // skip opening quote
			while (pos < source.Length && source[pos] != quote)
			{
				if (source[pos] == '\\' && pos + 1 < source.Length)
				{
					pos += 2; col += 2; // skip escape
				}
				else if (source[pos] == '\n') { line++; col = 0; pos++; }
				else if (source[pos] == '\r')
				{
					if (Peek(1) == '\n') pos++;
					pos++; line++; col = 0;
				}
				else
				{
					Advance();
				}
			}
			if (pos < source.Length) Advance(); // skip closing quote

			string text = source.Substring(start, pos - start);
			return new Token(NodeType.STRING_LITERATE, text, startLine, startCol);
		}

		#endregion

		#region Number

		private Token ReadNumber(int startLine, int startCol)
		{
			int start = pos;

			if (source[pos] == '0' && pos + 1 < source.Length)
			{
				char next = source[pos + 1];

				// Hex: 0x...
				if (next == 'x' || next == 'X')
				{
					pos += 2; col += 2;
					while (pos < source.Length && IsHexDigit(source[pos])) Advance();
					return new Token(NodeType.HEX_LITERATE, source.Substring(start, pos - start), startLine, startCol);
				}

				// Binary: 0b...
				if (next == 'b' || next == 'B')
				{
					pos += 2; col += 2;
					while (pos < source.Length && (source[pos] == '0' || source[pos] == '1')) Advance();
					return new Token(NodeType.BINARY_LITERATE, source.Substring(start, pos - start), startLine, startCol);
				}
			}

			// Decimal (integer or float)
			while (pos < source.Length && char.IsDigit(source[pos])) Advance();
			if (pos < source.Length && source[pos] == '.' && (pos + 1 >= source.Length || char.IsDigit(source[pos + 1])))
			{
				Advance(); // '.'
				while (pos < source.Length && char.IsDigit(source[pos])) Advance();
			}

			return new Token(NodeType.NUMBER_LITERATE, source.Substring(start, pos - start), startLine, startCol);
		}

		#endregion

		#region Identifier / Keyword

		private Token ReadIdentifierOrKeyword(int startLine, int startCol)
		{
			int start = pos;
			while (pos < source.Length && IsIdentPart(source[pos])) Advance();
			string word = source.Substring(start, pos - start);

			int type = word switch
			{
				"var" => NodeType.TYPE,
				"true" => NodeType.LIT_TRUE,
				"false" => NodeType.LIT_FALSE,
				"null" => NodeType.LIT_NULL,
				"undefined" => NodeType.UNDEFINED,
				"NaN" => NodeType.NAN,
				"this" => NodeType.THIS,
				"if" => NodeType.IF_STATEMENT,         // keyword token, parser remaps
				"else" => NodeType.ELSE,
				"for" => NodeType.FOR_STATEMENT,        // keyword token, parser remaps
				"while" => NodeType.WHILE_STATEMENT,    // keyword token, parser remaps
				"switch" => NodeType.SWITCH,
				"case" => NodeType.SWITCH_CASE,
				"default" => NodeType.SWITCH_CASE_ELSE,
				"break" => NodeType.BREAK,
				"continue" => NodeType.CONTINUE,
				"return" => NodeType.RETURN,
				"function" => NodeType.FUNCTION_DEFINE,
				"new" => NodeType.CREATE,
				"delete" => NodeType.DELETE_PROP,
				"typeof" => NodeType.TYPEOF,
				"instanceof" => NodeType.INSTANCEOF,
				"import" => NodeType.IMPORT,
				"try" => NodeType.TRY_CATCH,
				"catch" => NodeType.TRY_CATCH_CASE,
				"finally" => NodeType.TRY_CATCH_FINAL,
				"throw" => NodeType.TRY_CATCH_TRHOW,
				"in" => NodeType.FOREACH_STATEMENT,     // keyword token, parser remaps
				"template" => NodeType.TEMPLATE_DEFINE,
				"debugger" => NodeType.DEBUGGER,
				"private" => NodeType.PRIVATE,
				"protected" => NodeType.PROTECTED,
				"internal" => NodeType.INTERNAL,
				"public" => NodeType.PUBLIC,
				_ => NodeType.IDENTIFIER,
			};

			return new Token(type, word, startLine, startCol);
		}

		#endregion

		#region Operators / Punctuation

		private Token? ReadOperator(int startLine, int startCol)
		{
			char c = source[pos];
			char n = Peek(1);
			char n2 = Peek(2);

			// Three-character operators
			if (c == '=' && n == '=' && n2 == '=') { pos += 3; col += 3; return new Token(NodeType.STRICT_EQUALS, "===", startLine, startCol); }
			if (c == '!' && n == '=' && n2 == '=') { pos += 3; col += 3; return new Token(NodeType.STRICT_NOT_EQUALS, "!==", startLine, startCol); }
			if (c == '<' && n == '<' && n2 == '=') { pos += 3; col += 3; return new Token(NodeType.ASSIGN_LSHIFT, "<<=", startLine, startCol); }
			if (c == '>' && n == '>' && n2 == '=') { pos += 3; col += 3; return new Token(NodeType.ASSIGN_RSHIFT, ">>=", startLine, startCol); }

			// Arrow =>
			if (c == '=' && n == '>') { pos += 2; col += 2; return new Token(NodeType.LAMBDA_FUNCTION, "=>", startLine, startCol); }

			// Two-character operators
			if (c == '+' && n == '+') { pos += 2; col += 2; return new Token(NodeType.INCREMENT, "++", startLine, startCol); }
			if (c == '-' && n == '-') { pos += 2; col += 2; return new Token(NodeType.DECREMENT, "--", startLine, startCol); }
			if (c == '+' && n == '=') { pos += 2; col += 2; return new Token(NodeType.ASSIGN_PLUS, "+=", startLine, startCol); }
			if (c == '-' && n == '=') { pos += 2; col += 2; return new Token(NodeType.ASSIGN_MINUS, "-=", startLine, startCol); }
			if (c == '*' && n == '=') { pos += 2; col += 2; return new Token(NodeType.ASSIGN_MUL, "*=", startLine, startCol); }
			if (c == '/' && n == '=') { pos += 2; col += 2; return new Token(NodeType.ASSIGN_DIV, "/=", startLine, startCol); }
			if (c == '%' && n == '=') { pos += 2; col += 2; return new Token(NodeType.ASSIGN_REM, "%=", startLine, startCol); }
			if (c == '&' && n == '=') { pos += 2; col += 2; return new Token(NodeType.ASSIGN_AND, "&=", startLine, startCol); }
			if (c == '|' && n == '=') { pos += 2; col += 2; return new Token(NodeType.ASSIGN_OR, "|=", startLine, startCol); }
			if (c == '^' && n == '=') { pos += 2; col += 2; return new Token(NodeType.ASSIGN_REV, "^=", startLine, startCol); }
			if (c == '=' && n == '=') { pos += 2; col += 2; return new Token(NodeType.EQUALS, "==", startLine, startCol); }
			if (c == '!' && n == '=') { pos += 2; col += 2; return new Token(NodeType.NOT_EQUALS, "!=", startLine, startCol); }
			if (c == '>' && n == '=') { pos += 2; col += 2; return new Token(NodeType.GREAT_EQUALS, ">=", startLine, startCol); }
			if (c == '<' && n == '=') { pos += 2; col += 2; return new Token(NodeType.LESS_EQUALS, "<=", startLine, startCol); }
			if (c == '<' && n == '<') { pos += 2; col += 2; return new Token(NodeType.LSHIFT, "<<", startLine, startCol); }
			if (c == '>' && n == '>') { pos += 2; col += 2; return new Token(NodeType.RSHIFT, ">>", startLine, startCol); }
			if (c == '&' && n == '&') { pos += 2; col += 2; return new Token(NodeType.LOGICAL_AND, "&&", startLine, startCol); }
			if (c == '|' && n == '|') { pos += 2; col += 2; return new Token(NodeType.LOGICAL_OR, "||", startLine, startCol); }
			if (c == '/' && n == '>') { pos += 2; col += 2; return new Token(NodeType.TAG, "/>", startLine, startCol); }
			if (c == '<' && n == '/') { pos += 2; col += 2; return new Token(NodeType.TAG_NAME, "</", startLine, startCol); }

			// Single-character operators
			int type;
			switch (c)
			{
				case '+': type = NodeType.PLUS; break;
				case '-': type = NodeType.MINUS; break;
				case '*': type = NodeType.MUL; break;
				case '/': type = NodeType.DIV; break;
				case '%': type = NodeType.MOD; break;
				case '&': type = NodeType.AND; break;
				case '|': type = NodeType.OR; break;
				case '^': type = NodeType.XOR; break;
				case '~': type = NodeType.BITWISE_NOT; break;
				case '!': type = NodeType.NOT; break;
				case '=': type = NodeType.ASSIGNMENT; break;
				case '>': type = NodeType.GREAT_THAN; break;
				case '<': type = NodeType.LESS_THAN; break;
				case '(': type = NodeType.LPAREN; break;
				case ')': type = NodeType.RPAREN; break;
				case '[': type = NodeType.LBRACE; break;
				case ']': type = NodeType.RBRACE; break;
				case '{': type = NodeType.LCURLY; break;
				case '}': type = NodeType.RCURLY; break;
				case ',': type = NodeType.COMMA; break;
				case ':': type = NodeType.COLON; break;
				case '.': type = NodeType.DOT; break;
				case ';': type = NodeType.SEMI; break;
				case '?': type = NodeType.CONDITION; break;
				default: return null;
			}

			Advance();
			return new Token(type, c.ToString(), startLine, startCol);
		}

		#endregion

		#region Helpers

		private char Peek(int offset)
		{
			int idx = pos + offset;
			return idx < source.Length ? source[idx] : '\0';
		}

		private void Advance()
		{
			pos++;
			col++;
		}

		private static bool IsIdentStart(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '$';
		}

		private static bool IsIdentPart(char c)
		{
			return IsIdentStart(c) || (c >= '0' && c <= '9');
		}

		private static bool IsHexDigit(char c)
		{
			return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
		}

		#endregion
	}
}
