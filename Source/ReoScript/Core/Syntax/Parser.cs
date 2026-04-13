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
using unvell.ReoScript.Core.Statement;
using unvell.ReoScript.Reflection;

namespace unvell.ReoScript
{
	/// <summary>
	/// Hand-written recursive descent parser for ReoScript.
	/// Produces the same AST shape (SyntaxNode trees with NodeType int tags)
	/// that the former ANTLR-generated parser produced, so that the existing
	/// INodeParser dispatch in ScriptRunningMachine works without changes.
	/// </summary>
	sealed class ReoScriptHandwrittenParser
	{
		private List<Token> tokens;
		private int pos;

		internal Action<ErrorObject> CompilingErrorHandler;
		internal List<ErrorObject> CompilingErrors = new List<ErrorObject>();

		private Stack<StaticFunctionScope> localStack = new Stack<StaticFunctionScope>();
		internal StaticFunctionScope CurrentStack { get; set; }
		private StaticFunctionScope lastLocalScope;

		#region Public API

		public SyntaxNode ParseScript(string source)
		{
			var lexer = new ReoScriptLexerNew(source);
			tokens = lexer.Tokenize();
			pos = 0;

			PushLocalStack();
			var root = new SyntaxNode(0); // nil root
			while (!IsAtEnd())
			{
				int before = pos;
				var stmt = ParseStatement();
				if (stmt != null) root.AddChild(stmt);

				// Error recovery: if no progress was made, skip the current token
				if (pos == before && !IsAtEnd())
				{
					Advance();
				}
			}
			PopLocalStack();

			return root;
		}

		public SyntaxNode ParseExpression(string source)
		{
			var lexer = new ReoScriptLexerNew(source);
			tokens = lexer.Tokenize();
			pos = 0;

			if (Check(NodeType.LESS_THAN))
				return ParseTag();

			return ParseAssignmentExpression();
		}

		#endregion

		#region Statement Parsing

		private SyntaxNode ParseStatement()
		{
			if (Check(NodeType.IMPORT))
				return ParseImportStatement();

			if (Check(NodeType.TYPE))
				return ParseLocalVariableDeclaration();

			if (Check(NodeType.FUNCTION_DEFINE) && LookAhead(1).Type == NodeType.IDENTIFIER)
				return ParseFunctionDefine(0);

			if (Check(NodeType.TEMPLATE_DEFINE))
				return ParseTagTemplateDefine();

			return ParseEmbeddedStatement();
		}

		private SyntaxNode ParseImportStatement()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.IMPORT);

			var node = new SyntaxNode(NodeType.IMPORT, "IMPORT", sLine, sCol);

			if (Check(NodeType.STRING_LITERATE))
			{
				node.AddChild(MakeLeaf(Advance()));
				Expect(NodeType.SEMI);
				return node;
			}

			// namespace: IDENTIFIER ('.' (IDENTIFIER|'*'))*
			node.AddChild(MakeLeaf(Expect(NodeType.IDENTIFIER)));
			while (Match(NodeType.DOT))
			{
				node.AddChild(MakeLeaf(new Token(NodeType.DOT, ".", 0, 0)));
				if (Check(NodeType.MUL))
				{
					node.AddChild(MakeLeaf(Advance()));
				}
				else
				{
					node.AddChild(MakeLeaf(Expect(NodeType.IDENTIFIER)));
				}
			}
			Expect(NodeType.SEMI);
			return node;
		}

		private SyntaxNode ParseLocalVariableDeclaration()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.TYPE); // 'var'

			var first = ParseLocalVariableDeclarationAssignment();
			if (first == null) return null;

			// Parse additional declarations: var a = 1, b = 2;
			var extras = new List<SyntaxNode>();
			while (Match(NodeType.COMMA))
			{
				var extra = ParseLocalVariableDeclarationAssignment();
				if (extra != null) extras.Add(extra);
			}

			if (extras.Count == 0)
			{
				// Single declaration — return the VariableDefineNode directly
				// (ANTLR wraps in DECLARATION with TYPE child, but the interpreter
				//  dispatches on LOCAL_DECLARE_ASSIGNMENT which is the VariableDefineNode type)
				Expect(NodeType.SEMI);
				return first;
			}

			// Multiple declarations — wrap in DECLARATION node
			var decl = new SyntaxNode(NodeType.DECLARATION, "DECLARATION", sLine, sCol);
			decl.AddChild(first);
			foreach (var e in extras) decl.AddChild(e);
			Expect(NodeType.SEMI);
			return decl;
		}

		private VariableDefineNode ParseLocalVariableDeclarationAssignment()
		{
			var idToken = Expect(NodeType.IDENTIFIER);
			SyntaxNode initValue = null;

			if (Match(NodeType.ASSIGNMENT))
			{
				initValue = ParseExpression_Internal();
			}

			var vi = new VariableInfo
			{
				Name = idToken.Text,
				HasInitialValue = initValue != null,
				IsImplicitDeclaration = false,
				IsLocal = true,
				CharIndex = idToken.CharPosition,
				Line = idToken.Line,
				InitialValueTree = initValue,
			};

			var vdn = new VariableDefineNode
			{
				VariableInfo = vi,
			};

			CurrentStack.Variables.Add(vi);
			return vdn;
		}

		private SyntaxNode ParseFunctionDefine(int modifierToken)
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.FUNCTION_DEFINE); // 'function'
			var idToken = Expect(NodeType.IDENTIFIER);
			Expect(NodeType.LPAREN);
			string[] args = ParseParameterDeclarationList();
			Expect(NodeType.RPAREN);
			var body = ParseFunctionBody();

			var fi = new FunctionInfo
			{
				Name = idToken.Text,
				IsInner = localStack.Count > 1,
				Args = args,
				IsAnonymous = false,
				ScopeModifier = GetScopeModifier(modifierToken),
				BodyTree = body,
				CharIndex = sCol,
				Line = sLine,
				InnerScope = lastLocalScope,
				OuterScope = CurrentStack,
			};

			var fdn = new FunctionDefineNode { FunctionInfo = fi };
			CurrentStack.Functions.Add(fi);

			// Consume optional semicolon after function define
			Match(NodeType.SEMI);

			return fdn;
		}

		private string[] ParseParameterDeclarationList()
		{
			if (!Check(NodeType.IDENTIFIER)) return Array.Empty<string>();

			var args = new List<string>();
			args.Add(Advance().Text);
			while (Match(NodeType.COMMA))
			{
				args.Add(Expect(NodeType.IDENTIFIER).Text);
			}
			return args.ToArray();
		}

		private SyntaxNode ParseFunctionBody()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.LCURLY);
			PushLocalStack();
			var block = new SyntaxNode(NodeType.BLOCK, "BLOCK", sLine, sCol);
			while (!Check(NodeType.RCURLY) && !IsAtEnd())
			{
				var stmt = ParseStatement();
				if (stmt != null) block.AddChild(stmt);
			}
			PopLocalStack();
			Expect(NodeType.RCURLY);
			return block;
		}

		private SyntaxNode ParseEmbeddedStatement()
		{
			// empty statement
			if (Check(NodeType.SEMI))
			{
				Advance();
				return null;
			}

			// block
			if (Check(NodeType.LCURLY))
				return ParseBlock();

			// if
			if (Check(NodeType.IF_STATEMENT))
				return ParseIfElse();

			// for / foreach
			if (Check(NodeType.FOR_STATEMENT))
				return ParseForOrForeach();

			// while
			if (Check(NodeType.WHILE_STATEMENT))
				return ParseWhile();

			// switch
			if (Check(NodeType.SWITCH))
				return ParseSwitch();

			// try/catch/throw
			if (Check(NodeType.TRY_CATCH))
				return ParseTryCatch();
			if (Check(NodeType.TRY_CATCH_TRHOW))
				return ParseThrow();

			// return/break/continue
			if (Check(NodeType.RETURN) || Check(NodeType.BREAK) || Check(NodeType.CONTINUE))
				return ParseTerminalStatement();

			// debugger
			if (Check(NodeType.DEBUGGER))
			{
				Advance();
				Match(NodeType.SEMI);
				return new SyntaxNode(NodeType.DEBUGGER, "debugger");
			}

			// expression statement (assignment, invocation, etc.)
			var expr = ParseStatementExpression();
			if (!Match(NodeType.SEMI))
			{
				// Error recovery: skip to next semicolon or statement-starting token
				// without consuming the statement-starting token
				while (!IsAtEnd() && !Check(NodeType.SEMI) && !IsStatementStart())
				{
					Advance();
				}
				Match(NodeType.SEMI); // consume the semicolon if found
			}
			return expr;
		}

		private SyntaxNode ParseBlock()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.LCURLY);
			var block = new SyntaxNode(NodeType.BLOCK, "BLOCK", sLine, sCol);
			while (!Check(NodeType.RCURLY) && !IsAtEnd())
			{
				var stmt = ParseStatement();
				if (stmt != null) block.AddChild(stmt);
			}
			Expect(NodeType.RCURLY);
			return block;
		}

		#endregion

		#region Statement Expressions (assignments, invocations)

		private SyntaxNode ParseStatementExpression()
		{
			// new X(...)
			if (Check(NodeType.CREATE))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var expr = ParsePrimaryExpression();
				var node = new SyntaxNode(NodeType.CREATE, "CREATE", sLine, sCol);
				node.AddChild(expr);
				return node;
			}

			// delete X
			if (Check(NodeType.DELETE_PROP))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var expr = ParsePrimaryExpression();
				var node = new SyntaxNode(NodeType.DELETE_PROP, "DELETE_PROP", sLine, sCol);
				node.AddChild(expr);
				return node;
			}

			// ++expr
			if (Check(NodeType.INCREMENT))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				var op = Advance();
				var expr = ParsePrimaryExpression();
				var node = new SyntaxNode(NodeType.PRE_UNARY_STEP, "PRE_UNARY_STEP", sLine, sCol);
				node.AddChild(expr);
				node.AddChild(new SyntaxNode(NodeType.INCREMENT, "++"));
				return node;
			}

			// --expr
			if (Check(NodeType.DECREMENT))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				var op = Advance();
				var expr = ParsePrimaryExpression();
				var node = new SyntaxNode(NodeType.PRE_UNARY_STEP, "PRE_UNARY_STEP", sLine, sCol);
				node.AddChild(expr);
				node.AddChild(new SyntaxNode(NodeType.DECREMENT, "--"));
				return node;
			}

			// invocationExpression (primaryExpression with optional assignment suffix)
			return ParseInvocationExpression();
		}

		private SyntaxNode ParseInvocationExpression()
		{
			var id = ParsePrimaryExpression();

			// Assignment operators
			if (Check(NodeType.ASSIGNMENT))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var val = ParseExpression_Internal();
				var node = new SyntaxNode(NodeType.ASSIGNMENT, "ASSIGNMENT", sLine, sCol);
				node.AddChild(id);
				node.AddChild(val);
				return node;
			}

			// Compound assignment operators
			int? compoundOp = Current().Type switch
			{
				NodeType.ASSIGN_PLUS => NodeType.PLUS,
				NodeType.ASSIGN_MINUS => NodeType.MINUS,
				NodeType.ASSIGN_MUL => NodeType.MUL,
				NodeType.ASSIGN_DIV => NodeType.DIV,
				NodeType.ASSIGN_REM => NodeType.MOD,
				NodeType.ASSIGN_AND => NodeType.AND,
				NodeType.ASSIGN_OR => NodeType.OR,
				NodeType.ASSIGN_REV => NodeType.XOR,
				NodeType.ASSIGN_LSHIFT => NodeType.LSHIFT,
				NodeType.ASSIGN_RSHIFT => NodeType.RSHIFT,
				_ => null,
			};

			if (compoundOp.HasValue)
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance(); // consume +=, -=, etc.
				var val = ParseExpression_Internal();
				var binOp = new SyntaxNode(compoundOp.Value, GetNodeName(compoundOp.Value), sLine, sCol);
				binOp.AddChild(id);
				binOp.AddChild(val);
				var assign = new SyntaxNode(NodeType.ASSIGNMENT, "ASSIGNMENT", sLine, sCol);
				assign.AddChild(id);
				assign.AddChild(binOp);
				return assign;
			}

			// Post-increment/decrement
			if (Check(NodeType.INCREMENT))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var node = new SyntaxNode(NodeType.POST_UNARY_STEP, "POST_UNARY_STEP", sLine, sCol);
				node.AddChild(id);
				node.AddChild(new SyntaxNode(NodeType.INCREMENT, "++"));
				return node;
			}
			if (Check(NodeType.DECREMENT))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var node = new SyntaxNode(NodeType.POST_UNARY_STEP, "POST_UNARY_STEP", sLine, sCol);
				node.AddChild(id);
				node.AddChild(new SyntaxNode(NodeType.DECREMENT, "--"));
				return node;
			}

			return id;
		}

		#endregion

		#region Control Flow

		private SyntaxNode ParseIfElse()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.IF_STATEMENT); // 'if'
			Expect(NodeType.LPAREN);
			var condition = ParseConditionalOrExpression();
			Expect(NodeType.RPAREN);
			var thenStmt = ParseEmbeddedStatement();

			var node = new SyntaxNode(NodeType.IF_STATEMENT, "IF_STATEMENT", sLine, sCol);
			node.AddChild(condition);
			node.AddChild(thenStmt);

			if (Match(NodeType.ELSE))
			{
				node.AddChild(ParseEmbeddedStatement());
			}

			return node;
		}

		private SyntaxNode ParseForOrForeach()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.FOR_STATEMENT); // 'for'
			Expect(NodeType.LPAREN);

			// Distinguish for-in (foreach) from regular for
			// for (var x in expr) or for (x in expr)
			if (IsForeachPattern())
			{
				return ParseForeachBody(sLine, sCol);
			}

			return ParseForBody(sLine, sCol);
		}

		private bool IsForeachPattern()
		{
			// Look ahead to determine if this is a foreach:
			// for ( var? IDENTIFIER in expr )
			int saved = pos;
			bool isLocal = false;

			if (Check(NodeType.TYPE)) { saved = pos; Advance(); isLocal = true; }

			if (Check(NodeType.IDENTIFIER))
			{
				int idPos = pos;
				Advance();
				if (Check(NodeType.FOREACH_STATEMENT)) // 'in' keyword
				{
					pos = saved; // reset
					if (isLocal) pos = saved;
					return true;
				}
			}

			pos = saved;
			return false;
		}

		private SyntaxNode ParseForeachBody(int sLine, int sCol)
		{
			bool hasVar = Match(NodeType.TYPE);
			var idToken = Expect(NodeType.IDENTIFIER);
			Expect(NodeType.FOREACH_STATEMENT); // 'in'
			var expr = ParseExpression_Internal();
			Expect(NodeType.RPAREN);
			var body = ParseEmbeddedStatement();

			var node = new SyntaxNode(NodeType.FOREACH_STATEMENT, "FOREACH_STATEMENT", sLine, sCol);
			node.AddChild(MakeLeaf(idToken));
			node.AddChild(expr);
			node.AddChild(body);
			if (hasVar) node.AddChild(new SyntaxNode(NodeType.TYPE, "var"));

			return node;
		}

		private SyntaxNode ParseForBody(int sLine, int sCol)
		{
			// FOR_INIT
			var forInit = new SyntaxNode(NodeType.FOR_INIT, "FOR_INIT", sLine, sCol);
			if (!Check(NodeType.SEMI))
			{
				if (Check(NodeType.TYPE))
				{
					// var declarations in for init
					Advance(); // 'var'
					var first = ParseLocalVariableDeclarationAssignment();
					if (first != null) forInit.AddChild(first);
					while (Match(NodeType.COMMA))
					{
						var extra = ParseLocalVariableDeclarationAssignment();
						if (extra != null) forInit.AddChild(extra);
					}
				}
				else
				{
					forInit.AddChild(ParseStatementExpression());
					while (Match(NodeType.COMMA))
					{
						forInit.AddChild(ParseStatementExpression());
					}
				}
			}
			Expect(NodeType.SEMI);

			// FOR_CONDITION
			var forCond = new SyntaxNode(NodeType.FOR_CONDITION, "FOR_CONDITION", sLine, sCol);
			if (!Check(NodeType.SEMI))
			{
				forCond.AddChild(ParseConditionalOrExpression());
			}
			Expect(NodeType.SEMI);

			// FOR_ITERATOR
			var forIter = new SyntaxNode(NodeType.FOR_ITERATOR, "FOR_ITERATOR", sLine, sCol);
			if (!Check(NodeType.RPAREN))
			{
				forIter.AddChild(ParseStatementExpression());
				while (Match(NodeType.COMMA))
				{
					forIter.AddChild(ParseStatementExpression());
				}
			}
			Expect(NodeType.RPAREN);

			// FOR_BODY
			var forBody = new SyntaxNode(NodeType.FOR_BODY, "FOR_BODY", sLine, sCol);
			forBody.AddChild(ParseEmbeddedStatement());

			var node = new SyntaxNode(NodeType.FOR_STATEMENT, "FOR_STATEMENT", sLine, sCol);
			node.AddChild(forInit);
			node.AddChild(forCond);
			node.AddChild(forIter);
			node.AddChild(forBody);
			return node;
		}

		private SyntaxNode ParseWhile()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.WHILE_STATEMENT); // 'while'
			Expect(NodeType.LPAREN);
			var condition = ParseConditionalOrExpression();
			Expect(NodeType.RPAREN);
			var body = ParseEmbeddedStatement();

			// While is represented as FOR_STATEMENT with empty init/iterator (same as ANTLR grammar)
			var node = new SyntaxNode(NodeType.FOR_STATEMENT, "FOR_STATEMENT", sLine, sCol);
			node.AddChild(new SyntaxNode(NodeType.FOR_INIT, "FOR_INIT", sLine, sCol));
			var forCond = new SyntaxNode(NodeType.FOR_CONDITION, "FOR_CONDITION", sLine, sCol);
			forCond.AddChild(condition);
			node.AddChild(forCond);
			node.AddChild(new SyntaxNode(NodeType.FOR_ITERATOR, "FOR_ITERATOR", sLine, sCol));
			var forBody = new SyntaxNode(NodeType.FOR_BODY, "FOR_BODY", sLine, sCol);
			forBody.AddChild(body);
			node.AddChild(forBody);
			return node;
		}

		private SyntaxNode ParseSwitch()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.SWITCH); // 'switch'
			Expect(NodeType.LPAREN);
			var expr = ParseConditionalOrExpression();
			Expect(NodeType.RPAREN);
			Expect(NodeType.LCURLY);

			var node = new SyntaxNode(NodeType.SWITCH, "SWITCH", sLine, sCol);
			node.AddChild(expr);

			while (!Check(NodeType.RCURLY) && !IsAtEnd())
			{
				if (Check(NodeType.SWITCH_CASE)) // 'case'
				{
					Advance();
					var caseExpr = ParseExpression_Internal();
					Expect(NodeType.COLON);
					var caseNode = new SyntaxNode(NodeType.SWITCH_CASE, "SWITCH_CASE");
					caseNode.AddChild(caseExpr);
					node.AddChild(caseNode);
				}
				else if (Check(NodeType.SWITCH_CASE_ELSE)) // 'default'
				{
					Advance();
					Expect(NodeType.COLON);
					node.AddChild(new SyntaxNode(NodeType.SWITCH_CASE_ELSE, "SWITCH_CASE_ELSE"));
				}
				else
				{
					var stmt = ParseStatement();
					if (stmt != null) node.AddChild(stmt);
				}
			}

			Expect(NodeType.RCURLY);
			return node;
		}

		private SyntaxNode ParseTryCatch()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.TRY_CATCH); // 'try'
			var tryBlock = ParseBlock();

			SyntaxNode catchBlock = null;
			string errorVar = null;
			SyntaxNode finallyBlock = null;

			if (Check(NodeType.TRY_CATCH_CASE)) // 'catch'
			{
				Advance();
				if (Match(NodeType.LPAREN))
				{
					errorVar = Expect(NodeType.IDENTIFIER).Text;
					Expect(NodeType.RPAREN);
				}
				catchBlock = ParseBlock();
			}
			else if (Check(NodeType.TRY_CATCH_FINAL)) // 'finally'
			{
				Advance();
				finallyBlock = ParseBlock();
			}

			var node = new SyntaxNode(NodeType.TRY_CATCH, "TRY_CATCH", sLine, sCol);
			node.AddChild(tryBlock);

			var caseNode = new SyntaxNode(NodeType.TRY_CATCH_CASE, "TRY_CATCH_CASE");
			if (catchBlock != null) caseNode.AddChild(catchBlock);
			if (errorVar != null) caseNode.AddChild(new SyntaxNode(NodeType.IDENTIFIER, errorVar));
			node.AddChild(caseNode);

			var finalNode = new SyntaxNode(NodeType.TRY_CATCH_FINAL, "TRY_CATCH_FINAL");
			if (finallyBlock != null) finalNode.AddChild(finallyBlock);
			node.AddChild(finalNode);

			return node;
		}

		private SyntaxNode ParseThrow()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.TRY_CATCH_TRHOW); // 'throw'
			var expr = ParseExpression_Internal();
			Expect(NodeType.SEMI);
			var node = new SyntaxNode(NodeType.TRY_CATCH_TRHOW, "TRY_CATCH_TRHOW", sLine, sCol);
			node.AddChild(expr);
			return node;
		}

		private SyntaxNode ParseTerminalStatement()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			var tok = Advance();
			SyntaxNode node;

			switch (tok.Type)
			{
				case NodeType.RETURN:
					node = new SyntaxNode(NodeType.RETURN, "RETURN", sLine, sCol);
					if (!Check(NodeType.SEMI) && !Check(NodeType.RCURLY) && !IsAtEnd())
					{
						node.AddChild(ParseExpression_Internal());
					}
					break;
				case NodeType.BREAK:
					node = new SyntaxNode(NodeType.BREAK, "BREAK", sLine, sCol);
					break;
				case NodeType.CONTINUE:
					node = new SyntaxNode(NodeType.CONTINUE, "CONTINUE", sLine, sCol);
					break;
				default:
					node = new SyntaxNode(tok.Type, tok.Text, sLine, sCol);
					break;
			}

			Expect(NodeType.SEMI);
			return node;
		}

		#endregion

		#region Expression Parsing (Precedence Climbing)

		private SyntaxNode ParseExpression_Internal()
		{
			if (Check(NodeType.LESS_THAN) && IsTagStart())
				return ParseTag();

			return ParseAssignmentExpression();
		}

		private SyntaxNode ParseAssignmentExpression()
		{
			var left = ParseConditionalExpression();

			if (Check(NodeType.ASSIGNMENT))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var right = ParseExpression_Internal();
				var node = new SyntaxNode(NodeType.ASSIGNMENT, "ASSIGNMENT", sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				return node;
			}

			return left;
		}

		private SyntaxNode ParseConditionalExpression()
		{
			var expr = ParseConditionalOrExpression();

			if (Match(NodeType.CONDITION)) // '?'
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				var thenExpr = ParseExpression_Internal();
				Expect(NodeType.COLON);
				var elseExpr = ParseExpression_Internal();
				var node = new SyntaxNode(NodeType.CONDITION, "CONDITION", sLine, sCol);
				node.AddChild(expr);
				node.AddChild(thenExpr);
				node.AddChild(elseExpr);
				return node;
			}

			return expr;
		}

		private SyntaxNode ParseConditionalOrExpression()
		{
			var left = ParseConditionalAndExpression();
			while (Check(NodeType.LOGICAL_OR))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var right = ParseConditionalAndExpression();
				var node = new SyntaxNode(NodeType.LOGICAL_OR, "||", sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseConditionalAndExpression()
		{
			var left = ParseInclusiveOrExpression();
			while (Check(NodeType.LOGICAL_AND))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var right = ParseInclusiveOrExpression();
				var node = new SyntaxNode(NodeType.LOGICAL_AND, "&&", sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseInclusiveOrExpression()
		{
			var left = ParseExclusiveOrExpression();
			while (Check(NodeType.OR))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var right = ParseExclusiveOrExpression();
				var node = new SyntaxNode(NodeType.OR, "|", sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseExclusiveOrExpression()
		{
			var left = ParseAndExpression();
			while (Check(NodeType.XOR))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var right = ParseAndExpression();
				var node = new SyntaxNode(NodeType.XOR, "^", sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseAndExpression()
		{
			var left = ParseInstanceOfExpression();
			while (Check(NodeType.AND))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var right = ParseInstanceOfExpression();
				var node = new SyntaxNode(NodeType.AND, "&", sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseInstanceOfExpression()
		{
			var left = ParseEqualityExpression();
			if (Check(NodeType.INSTANCEOF))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				Advance();
				var right = ParseExpression_Internal();
				var node = new SyntaxNode(NodeType.INSTANCEOF, "instanceof", sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				return node;
			}
			return left;
		}

		private SyntaxNode ParseEqualityExpression()
		{
			var left = ParseRelationalExpression();
			while (Check(NodeType.EQUALS) || Check(NodeType.NOT_EQUALS)
				|| Check(NodeType.STRICT_EQUALS) || Check(NodeType.STRICT_NOT_EQUALS))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				var op = Advance();
				var right = ParseRelationalExpression();
				var node = new SyntaxNode(op.Type, op.Text, sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseRelationalExpression()
		{
			var left = ParseShiftExpression();
			while (Check(NodeType.GREAT_THAN) || Check(NodeType.GREAT_EQUALS)
				|| Check(NodeType.LESS_THAN) || Check(NodeType.LESS_EQUALS))
			{
				// Avoid consuming '<' that starts a tag closing </
				if (Check(NodeType.LESS_THAN) && LookAhead(1).Type == NodeType.DIV)
					break;

				int sLine = Current().Line, sCol = Current().CharPosition;
				var op = Advance();
				var right = ParseShiftExpression();
				var node = new SyntaxNode(op.Type, op.Text, sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseShiftExpression()
		{
			var left = ParseAdditiveExpression();
			while (Check(NodeType.LSHIFT) || Check(NodeType.RSHIFT))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				var op = Advance();
				var right = ParseAdditiveExpression();
				var node = new SyntaxNode(op.Type, op.Text, sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseAdditiveExpression()
		{
			var left = ParseMultiplicativeExpression();
			while (Check(NodeType.PLUS) || Check(NodeType.MINUS))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				var op = Advance();
				var right = ParseMultiplicativeExpression();
				var node = new SyntaxNode(op.Type, op.Text, sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseMultiplicativeExpression()
		{
			var left = ParseUnaryExpression();
			while (Check(NodeType.MUL) || Check(NodeType.DIV) || Check(NodeType.MOD))
			{
				int sLine = Current().Line, sCol = Current().CharPosition;
				var op = Advance();
				var right = ParseUnaryExpression();
				var node = new SyntaxNode(op.Type, op.Text, sLine, sCol);
				node.AddChild(left);
				node.AddChild(right);
				left = node;
			}
			return left;
		}

		private SyntaxNode ParseUnaryExpression()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;

			// Pre-increment/decrement
			if (Check(NodeType.INCREMENT))
			{
				Advance();
				var expr = ParsePrimaryExpression();
				var node = new SyntaxNode(NodeType.PRE_UNARY_STEP, "PRE_UNARY_STEP", sLine, sCol);
				node.AddChild(expr);
				node.AddChild(new SyntaxNode(NodeType.INCREMENT, "++"));
				return node;
			}
			if (Check(NodeType.DECREMENT))
			{
				Advance();
				var expr = ParsePrimaryExpression();
				var node = new SyntaxNode(NodeType.PRE_UNARY_STEP, "PRE_UNARY_STEP", sLine, sCol);
				node.AddChild(expr);
				node.AddChild(new SyntaxNode(NodeType.DECREMENT, "--"));
				return node;
			}

			// new
			if (Check(NodeType.CREATE))
			{
				Advance();
				var expr = ParsePrimaryExpression();
				var node = new SyntaxNode(NodeType.CREATE, "CREATE", sLine, sCol);
				node.AddChild(expr);
				return node;
			}

			// Unary +, -, !, ~
			if (Check(NodeType.PLUS) || Check(NodeType.MINUS) || Check(NodeType.NOT) || Check(NodeType.BITWISE_NOT))
			{
				var op = Advance();
				var node = new SyntaxNode(NodeType.PRE_UNARY, "PRE_UNARY", sLine, sCol);
				// Map BITWISE_NOT back to NOT for the AST (ANTLR used NOT for both)
				int opType = op.Type == NodeType.BITWISE_NOT ? NodeType.NOT : op.Type;
				node.AddChild(new SyntaxNode(opType, op.Text));
				var expr = ParseUnaryExpression();
				node.AddChild(expr);
				return node;
			}

			// typeof
			if (Check(NodeType.TYPEOF))
			{
				Advance();
				var expr = ParseUnaryExpression();
				var node = new SyntaxNode(NodeType.TYPEOF, "TYPEOF", sLine, sCol);
				node.AddChild(expr);
				return node;
			}

			// Primary expression with optional post-increment/decrement
			var primary = ParsePrimaryExpression();

			if (Check(NodeType.INCREMENT))
			{
				Advance();
				var node = new SyntaxNode(NodeType.POST_UNARY_STEP, "POST_UNARY_STEP", sLine, sCol);
				node.AddChild(primary);
				node.AddChild(new SyntaxNode(NodeType.INCREMENT, "++"));
				return node;
			}
			if (Check(NodeType.DECREMENT))
			{
				Advance();
				var node = new SyntaxNode(NodeType.POST_UNARY_STEP, "POST_UNARY_STEP", sLine, sCol);
				node.AddChild(primary);
				node.AddChild(new SyntaxNode(NodeType.DECREMENT, "--"));
				return node;
			}

			return primary;
		}

		#endregion

		#region Primary Expression

		private SyntaxNode ParsePrimaryExpression()
		{
			SyntaxNode expr;
			int sLine = Current().Line, sCol = Current().CharPosition;

			// Object literal at start: { key: value }
			if (Check(NodeType.LCURLY) && IsObjectLiteral())
			{
				expr = ParseObjectLiteral();
				// Object literal can be followed by property access
				while (Check(NodeType.DOT))
				{
					Advance();
					var propToken = Expect(NodeType.IDENTIFIER);
					var prop = new SyntaxNode(NodeType.PROPERTY_ACCESS, "PROPERTY_ACCESS", sLine, sCol);
					prop.AddChild(expr);
					prop.AddChild(MakeLeaf(propToken));
					expr = prop;
				}
				return expr;
			}

			// Core primary: variable, literal, '(' expression ')', array, anonymous function
			if (Check(NodeType.IDENTIFIER))
			{
				// Check for arrow function: id => ...
				if (LookAhead(1).Type == NodeType.LAMBDA_FUNCTION)
				{
					expr = ParseAnonymousFunction();
				}
				else
				{
					expr = MakeLeaf(Advance());
				}
			}
			else if (Check(NodeType.THIS))
			{
				expr = MakeLeaf(Advance());
			}
			else if (IsConstLiteral())
			{
				expr = ParseConstLiteral();
			}
			else if (Check(NodeType.LBRACE))
			{
				expr = ParseArrayLiteral();
			}
			else if (Check(NodeType.FUNCTION_DEFINE) && LookAhead(1).Type == NodeType.LPAREN)
			{
				expr = ParseAnonymousFunction();
			}
			else if (Check(NodeType.LPAREN))
			{
				// Check for arrow function: (...) => ...
				if (IsArrowFunction())
				{
					expr = ParseAnonymousFunction();
				}
				else
				{
					Advance(); // '('
					expr = ParseExpression_Internal();
					Expect(NodeType.RPAREN);
				}
			}
			else
			{
				// Unexpected token
				var tok = Current();
				ReportError($"unexpected token '{tok.Text}'", tok.Line, tok.CharPosition);
				Advance();
				return new SyntaxNode(NodeType.IDENTIFIER, "?error?");
			}

			// Postfix: function call, property access, array access, combine object
			return ParsePostfixChain(expr);
		}

		private SyntaxNode ParsePostfixChain(SyntaxNode expr)
		{
			while (true)
			{
				int sLine = Current().Line, sCol = Current().CharPosition;

				if (Check(NodeType.LPAREN))
				{
					// Function call
					Advance(); // '('
					var call = new SyntaxNode(NodeType.FUNCTION_CALL, "FUNCTION_CALL", sLine, sCol);
					call.AddChild(expr);
					if (!Check(NodeType.RPAREN))
					{
						var argList = new SyntaxNode(NodeType.ARGUMENT_LIST, "ARGUMENT_LIST", sLine, sCol);
						argList.AddChild(ParseExpression_Internal());
						while (Match(NodeType.COMMA))
						{
							argList.AddChild(ParseExpression_Internal());
						}
						call.AddChild(argList);
					}
					Expect(NodeType.RPAREN);
					expr = call;
				}
				else if (Check(NodeType.DOT))
				{
					Advance();
					var propToken = Expect(NodeType.IDENTIFIER);
					var prop = new SyntaxNode(NodeType.PROPERTY_ACCESS, "PROPERTY_ACCESS", sLine, sCol);
					prop.AddChild(expr);
					prop.AddChild(MakeLeaf(propToken));
					expr = prop;
				}
				else if (Check(NodeType.LBRACE))
				{
					Advance(); // '['
					var idx = ParseExpression_Internal();
					Expect(NodeType.RBRACE); // ']'
					var access = new SyntaxNode(NodeType.ARRAY_ACCESS, "ARRAY_ACCESS", sLine, sCol);
					access.AddChild(expr);
					access.AddChild(idx);
					expr = access;
				}
				else if (Check(NodeType.LCURLY) && IsObjectLiteral())
				{
					// Combine object: expr { ... }
					var objLit = ParseObjectLiteral();
					var combine = new SyntaxNode(NodeType.COMBINE_OBJECT, "COMBINE_OBJECT", sLine, sCol);
					combine.AddChild(expr);
					combine.AddChild(objLit);
					expr = combine;
				}
				else
				{
					break;
				}
			}

			return expr;
		}

		#endregion

		#region Literals

		private bool IsConstLiteral()
		{
			int t = Current().Type;
			return t == NodeType.NUMBER_LITERATE || t == NodeType.HEX_LITERATE
				|| t == NodeType.BINARY_LITERATE || t == NodeType.STRING_LITERATE
				|| t == NodeType.LIT_TRUE || t == NodeType.LIT_FALSE
				|| t == NodeType.LIT_NULL || t == NodeType.UNDEFINED
				|| t == NodeType.NAN;
		}

		private SyntaxNode ParseConstLiteral()
		{
			var tok = Advance();
			int sLine = tok.Line, sCol = tok.CharPosition;

			switch (tok.Type)
			{
				case NodeType.NUMBER_LITERATE:
					{
						double val = Convert.ToDouble(tok.Text, System.Globalization.CultureInfo.InvariantCulture);
						var constNode = new ConstValueNode(val, NodeType.NUMBER_LITERATE, sLine, sCol);
						var wrapper = new SyntaxNode(NodeType.CONST_VALUE, "CONST_VALUE", sLine, sCol);
						wrapper.AddChild(constNode);
						return wrapper;
					}
				case NodeType.HEX_LITERATE:
					{
						double val = (double)Convert.ToInt32(tok.Text.Substring(2), 16);
						var constNode = new ConstValueNode(val, NodeType.NUMBER_LITERATE, sLine, sCol);
						var wrapper = new SyntaxNode(NodeType.CONST_VALUE, "CONST_VALUE", sLine, sCol);
						wrapper.AddChild(constNode);
						return wrapper;
					}
				case NodeType.BINARY_LITERATE:
					{
						double val = (double)Convert.ToInt32(tok.Text.Substring(2), 2);
						var constNode = new ConstValueNode(val, NodeType.NUMBER_LITERATE, sLine, sCol);
						var wrapper = new SyntaxNode(NodeType.CONST_VALUE, "CONST_VALUE", sLine, sCol);
						wrapper.AddChild(constNode);
						return wrapper;
					}
				case NodeType.STRING_LITERATE:
					{
						string str = tok.Text.Substring(1, tok.Text.Length - 2);
						str = ScriptRunningMachine.ConvertEscapeLiterals(str);
						var constNode = new ConstValueNode(str, NodeType.STRING_LITERATE, sLine, sCol);
						var wrapper = new SyntaxNode(NodeType.CONST_VALUE, "CONST_VALUE", sLine, sCol);
						wrapper.AddChild(constNode);
						return wrapper;
					}
				case NodeType.LIT_TRUE:
					{
						var wrapper = new SyntaxNode(NodeType.CONST_VALUE, "CONST_VALUE", sLine, sCol);
						wrapper.AddChild(new SyntaxNode(NodeType.LIT_TRUE, "true", sLine, sCol));
						return wrapper;
					}
				case NodeType.LIT_FALSE:
					{
						var wrapper = new SyntaxNode(NodeType.CONST_VALUE, "CONST_VALUE", sLine, sCol);
						wrapper.AddChild(new SyntaxNode(NodeType.LIT_FALSE, "false", sLine, sCol));
						return wrapper;
					}
				case NodeType.LIT_NULL:
				case NodeType.UNDEFINED:
					{
						var wrapper = new SyntaxNode(NodeType.CONST_VALUE, "CONST_VALUE", sLine, sCol);
						wrapper.AddChild(new SyntaxNode(tok.Type, tok.Text, sLine, sCol));
						return wrapper;
					}
				case NodeType.NAN:
					return new SyntaxNode(NodeType.NAN, "NaN", sLine, sCol);
			}

			return new SyntaxNode(tok.Type, tok.Text, sLine, sCol);
		}

		private SyntaxNode ParseArrayLiteral()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.LBRACE); // '['
			var node = new SyntaxNode(NodeType.ARRAY_LITERAL, "ARRAY_LITERAL", sLine, sCol);
			if (!Check(NodeType.RBRACE))
			{
				node.AddChild(ParseExpression_Internal());
				while (Match(NodeType.COMMA))
				{
					if (Check(NodeType.RBRACE)) break; // trailing comma
					node.AddChild(ParseExpression_Internal());
				}
			}
			Expect(NodeType.RBRACE); // ']'
			return node;
		}

		private SyntaxNode ParseObjectLiteral()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.LCURLY); // '{'
			var node = new SyntaxNode(NodeType.OBJECT_LITERAL, "OBJECT_LITERAL", sLine, sCol);

			if (!Check(NodeType.RCURLY))
			{
				ParseKeyPair(node);
				while (Match(NodeType.COMMA))
				{
					if (Check(NodeType.RCURLY)) break; // trailing comma
					ParseKeyPair(node);
				}
			}

			Expect(NodeType.RCURLY); // '}'
			return node;
		}

		private void ParseKeyPair(SyntaxNode parent)
		{
			// key: either identifier or string
			SyntaxNode key;
			if (Check(NodeType.STRING_LITERATE))
			{
				key = MakeLeaf(Advance());
			}
			else
			{
				key = MakeLeaf(Expect(NodeType.IDENTIFIER));
			}
			Expect(NodeType.COLON);
			var value = ParseExpression_Internal();
			parent.AddChild(key);
			parent.AddChild(value);
		}

		private bool IsObjectLiteral()
		{
			// Distinguish object literal { key: value } from block { statement }
			// Look ahead: { IDENTIFIER : ... } or { STRING : ... } or { }
			if (!Check(NodeType.LCURLY)) return false;

			int saved = pos;
			pos++; // skip '{'

			if (Check(NodeType.RCURLY)) { pos = saved; return true; } // empty object {}

			bool result = false;
			if (Check(NodeType.IDENTIFIER) || Check(NodeType.STRING_LITERATE))
			{
				pos++;
				result = Check(NodeType.COLON);
			}

			pos = saved;
			return result;
		}

		#endregion

		#region Anonymous / Arrow Functions

		private bool IsArrowFunction()
		{
			// Check if '(' ... ')' '=>' pattern
			int saved = pos;
			if (!Check(NodeType.LPAREN)) return false;

			pos++; // skip '('
			int depth = 1;
			while (depth > 0 && !IsAtEnd())
			{
				if (Check(NodeType.LPAREN)) depth++;
				else if (Check(NodeType.RPAREN)) depth--;
				if (depth > 0) pos++;
			}
			if (IsAtEnd()) { pos = saved; return false; }
			pos++; // skip closing ')'
			bool isArrow = Check(NodeType.LAMBDA_FUNCTION);
			pos = saved;
			return isArrow;
		}

		private bool IsTagStart()
		{
			// < followed by IDENTIFIER (tag) not comparison operator
			if (!Check(NodeType.LESS_THAN)) return false;
			int next = pos + 1;
			if (next >= tokens.Count) return false;
			return tokens[next].Type == NodeType.IDENTIFIER;
		}

		private SyntaxNode ParseAnonymousFunction()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;

			// function (...) { ... }
			if (Check(NodeType.FUNCTION_DEFINE))
			{
				Advance(); // 'function'
				Expect(NodeType.LPAREN);
				string[] args = ParseParameterDeclarationList();
				Expect(NodeType.RPAREN);
				var body = ParseFunctionBody();
				return MakeAnonymousFunctionNode(null, args, body, sLine, sCol);
			}

			// (...) => { ... } or (...) => expr
			if (Check(NodeType.LPAREN))
			{
				Advance(); // '('
				string[] args = ParseParameterDeclarationList();
				Expect(NodeType.RPAREN);
				Expect(NodeType.LAMBDA_FUNCTION); // '=>'

				if (Check(NodeType.LCURLY))
				{
					var body = ParseFunctionBody();
					return MakeAnonymousFunctionNode(null, args, body, sLine, sCol);
				}
				else
				{
					var expr = ParseAssignmentExpression();
					var retNode = new SyntaxNode(NodeType.RETURN, "RETURN");
					retNode.AddChild(expr);
					return MakeAnonymousFunctionNode(null, null, retNode, sLine, sCol, args);
				}
			}

			// id => { ... } or id => expr
			if (Check(NodeType.IDENTIFIER))
			{
				string arg1 = Advance().Text;
				Expect(NodeType.LAMBDA_FUNCTION); // '=>'

				if (Check(NodeType.LCURLY))
				{
					var body = ParseFunctionBody();
					return MakeAnonymousFunctionNode(arg1, null, body, sLine, sCol);
				}
				else
				{
					var expr = ParseAssignmentExpression();
					var retNode = new SyntaxNode(NodeType.RETURN, "RETURN");
					retNode.AddChild(expr);
					return MakeAnonymousFunctionNode(arg1, null, retNode, sLine, sCol);
				}
			}

			ReportError("expected anonymous function", sLine, sCol);
			return new SyntaxNode(NodeType.IDENTIFIER, "?error?");
		}

		private AnonymousFunctionDefineNode MakeAnonymousFunctionNode(
			string arg1, string[] args, SyntaxNode body, int line, int charIndex,
			string[] overrideArgs = null)
		{
			var fi = new FunctionInfo
			{
				Name = "<anonymous>",
				Args = overrideArgs ?? (arg1 != null ? new string[] { arg1 } : args ?? Array.Empty<string>()),
				IsAnonymous = true,
				ScopeModifier = MemberScopeModifier.Public,
				BodyTree = body,
				CharIndex = charIndex,
				Line = line,
				InnerScope = lastLocalScope,
				OuterScope = CurrentStack,
			};

			var afdn = new AnonymousFunctionDefineNode { FunctionInfo = fi };

			if (body.Type != NodeType.RETURN)
			{
				CurrentStack.Functions.Add(fi);
			}

			return afdn;
		}

		#endregion

		#region Tags

		private SyntaxNode ParseTag()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			Expect(NodeType.LESS_THAN); // '<'

			// Optional namespace: ns:name
			string ns = null;
			var nameToken = Expect(NodeType.IDENTIFIER);

			if (Match(NodeType.COLON))
			{
				ns = nameToken.Text;
				nameToken = Expect(NodeType.IDENTIFIER);
			}

			// TAG_NAME node
			var tagNameNode = new SyntaxNode(NodeType.TAG_NAME, "TAG_NAME");
			tagNameNode.AddChild(new SyntaxNode(NodeType.IDENTIFIER, nameToken.Text));
			if (ns != null)
				tagNameNode.AddChild(new SyntaxNode(NodeType.IDENTIFIER, ns));

			// Attributes
			var attrList = new SyntaxNode(NodeType.TAG_ATTR_LIST, "TAG_ATTR_LIST");
			while (Check(NodeType.IDENTIFIER))
			{
				var attrName = Advance();
				Expect(NodeType.ASSIGNMENT); // '='
				var attrValue = ParseUnaryExpression();
				var attr = new SyntaxNode(NodeType.TAG_ATTR, "TAG_ATTR");
				attr.AddChild(new SyntaxNode(NodeType.IDENTIFIER, attrName.Text));
				attr.AddChild(attrValue);
				attrList.AddChild(attr);
			}

			var tag = new SyntaxNode(NodeType.TAG, "TAG", sLine, sCol);
			tag.AddChild(tagNameNode);
			tag.AddChild(attrList);

			// Self-closing: /> or children: > ... </name>
			if (Check(NodeType.TAG)) // '/>'
			{
				Advance();
			}
			else if (Check(NodeType.GREAT_THAN))
			{
				Advance(); // '>'
				// Parse children (statements and nested tags)
				while (!Check(NodeType.TAG_NAME) && !IsAtEnd()) // '</'
				{
					if (Check(NodeType.LESS_THAN) && IsTagStart())
					{
						tag.AddChild(ParseTag());
					}
					else
					{
						var stmt = ParseStatement();
						if (stmt != null) tag.AddChild(stmt);
					}
				}
				// Closing tag: </ ns:name >
				if (Check(NodeType.TAG_NAME)) // '</'
				{
					Advance();
					if (Check(NodeType.IDENTIFIER))
					{
						Advance(); // name or ns
						if (Match(NodeType.COLON)) { if (Check(NodeType.IDENTIFIER)) Advance(); }
					}
					Expect(NodeType.GREAT_THAN); // '>'
				}
			}

			return tag;
		}

		private SyntaxNode ParseTagTemplateDefine()
		{
			int sLine = Current().Line, sCol = Current().CharPosition;
			// optional modifier already consumed or not present
			Expect(NodeType.TEMPLATE_DEFINE); // 'template'
			Expect(NodeType.LESS_THAN); // '<'
			var typeName = Expect(NodeType.IDENTIFIER);
			Expect(NodeType.GREAT_THAN); // '>'

			string[] args = null;
			if (Match(NodeType.LPAREN))
			{
				args = ParseParameterDeclarationList();
				Expect(NodeType.RPAREN);
			}

			var tagNode = ParseTag();

			var node = new SyntaxNode(NodeType.TEMPLATE_DEFINE, "TEMPLATE_DEFINE", sLine, sCol);
			node.AddChild(new SyntaxNode(NodeType.IDENTIFIER, typeName.Text));

			var paramNode = new SyntaxNode(NodeType.PARAMETER_DEFINES, "PARAMETER_DEFINES");
			if (args != null)
			{
				foreach (var a in args) paramNode.AddChild(new SyntaxNode(NodeType.IDENTIFIER, a));
			}
			node.AddChild(paramNode);
			node.AddChild(tagNode);

			return node;
		}

		#endregion

		#region Scope Stack Management

		private void PushLocalStack()
		{
			localStack.Push(CurrentStack = new StaticFunctionScope());
		}

		private void PopLocalStack()
		{
			if (localStack.Count > 1)
				lastLocalScope = localStack.Pop();

			CurrentStack = localStack.Peek();
		}

		private static MemberScopeModifier GetScopeModifier(int tokenType)
		{
			return tokenType switch
			{
				NodeType.INTERNAL => MemberScopeModifier.Internal,
				NodeType.PROTECTED => MemberScopeModifier.Protected,
				NodeType.PRIVATE => MemberScopeModifier.Private,
				_ => MemberScopeModifier.Public,
			};
		}

		#endregion

		#region Token Helpers

		private Token Current()
		{
			if (pos < tokens.Count) return tokens[pos];
			return new Token(NodeType.EOF, "<EOF>", 0, 0);
		}

		private Token LookAhead(int offset)
		{
			int idx = pos + offset;
			if (idx < tokens.Count) return tokens[idx];
			return new Token(NodeType.EOF, "<EOF>", 0, 0);
		}

		private bool IsAtEnd()
		{
			return pos >= tokens.Count || tokens[pos].Type == NodeType.EOF;
		}

		private bool Check(int type)
		{
			return !IsAtEnd() && tokens[pos].Type == type;
		}

		private bool Match(int type)
		{
			if (Check(type)) { pos++; return true; }
			return false;
		}

		private Token Advance()
		{
			var tok = Current();
			pos++;
			return tok;
		}

		private Token Expect(int type)
		{
			if (Check(type)) return Advance();

			var tok = Current();
			ReportError($"expected {GetNodeName(type)}, got '{tok.Text}'", tok.Line, tok.CharPosition);

			// Don't advance past the end
			if (!IsAtEnd()) pos++;
			return new Token(type, "", tok.Line, tok.CharPosition);
		}

		private SyntaxNode MakeLeaf(Token tok)
		{
			return new SyntaxNode(tok.Type, tok.Text, tok.Line, tok.CharPosition);
		}

		#endregion

		#region Error Reporting

		private void ReportError(string message, int line, int charPos)
		{
			var e = new ErrorObject
			{
				Message = $"syntax error at char {charPos} on line {line}, {message}",
				CharIndex = charPos,
				Line = line,
			};

			CompilingErrors.Add(e);
			CompilingErrorHandler?.Invoke(e);
		}

		#endregion

		#region Utilities

		private bool IsStatementStart()
		{
			int t = Current().Type;
			return t == NodeType.TYPE || t == NodeType.FUNCTION_DEFINE || t == NodeType.IF_STATEMENT
				|| t == NodeType.FOR_STATEMENT || t == NodeType.WHILE_STATEMENT || t == NodeType.SWITCH
				|| t == NodeType.TRY_CATCH || t == NodeType.TRY_CATCH_TRHOW
				|| t == NodeType.RETURN || t == NodeType.BREAK || t == NodeType.CONTINUE
				|| t == NodeType.IMPORT || t == NodeType.LCURLY || t == NodeType.RCURLY;
		}

		private static string GetNodeName(int type)
		{
			return type switch
			{
				NodeType.IDENTIFIER => "identifier",
				NodeType.LPAREN => "'('",
				NodeType.RPAREN => "')'",
				NodeType.LCURLY => "'{'",
				NodeType.RCURLY => "'}'",
				NodeType.LBRACE => "'['",
				NodeType.RBRACE => "']'",
				NodeType.SEMI => "';'",
				NodeType.COLON => "':'",
				NodeType.DOT => "'.'",
				NodeType.COMMA => "','",
				NodeType.ASSIGNMENT => "'='",
				NodeType.LESS_THAN => "'<'",
				NodeType.GREAT_THAN => "'>'",
				NodeType.PLUS => "'+'",
				NodeType.MINUS => "'-'",
				NodeType.MUL => "'*'",
				NodeType.DIV => "'/'",
				_ => $"token({type})",
			};
		}

		#endregion
	}
}
