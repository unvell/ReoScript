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
	/// Node type constants for the ReoScript AST.
	/// Values are kept identical to the former ANTLR-generated ReoScriptLexer
	/// constants so that existing INodeParser dispatch tables work unchanged.
	/// </summary>
	static class NodeType
	{
		public const int EOF = -1;

		// Operators
		public const int AND = 4;
		public const int ANONYMOUS_FUNCTION = 5;
		public const int ARGUMENT_LIST = 6;
		public const int ARRAY_ACCESS = 7;
		public const int ARRAY_LITERAL = 8;
		public const int ASSIGNMENT = 9;
		public const int ASSIGN_AND = 10;
		public const int ASSIGN_DIV = 11;
		public const int ASSIGN_LSHIFT = 12;
		public const int ASSIGN_MINUS = 13;
		public const int ASSIGN_MUL = 14;
		public const int ASSIGN_OR = 15;
		public const int ASSIGN_PLUS = 16;
		public const int ASSIGN_REM = 17;
		public const int ASSIGN_REV = 18;
		public const int ASSIGN_RSHIFT = 19;
		public const int BINARY_LITERATE = 20;
		public const int BLOCK = 21;
		public const int BREAK = 22;
		public const int CLASS = 23;
		public const int COLON = 24;
		public const int COMBINE_OBJECT = 25;
		public const int COMMA = 26;
		public const int COMMENT = 27;
		public const int CONDITION = 28;
		public const int CONST_VALUE = 29;
		public const int CONTINUE = 30;
		public const int CREATE = 31;
		public const int DECLARATION = 32;
		public const int DECREMENT = 33;
		public const int DELETE_PROP = 34;
		public const int DIV = 35;
		public const int DOT = 36;
		public const int ELSE = 37;
		public const int EQUALS = 38;
		public const int ESCAPE_SEQUENCE = 39;
		public const int FOREACH_STATEMENT = 40;
		public const int FOR_BODY = 41;
		public const int FOR_CONDITION = 42;
		public const int FOR_INIT = 43;
		public const int FOR_ITERATOR = 44;
		public const int FOR_STATEMENT = 45;
		public const int FUNCTION_CALL = 46;
		public const int FUNCTION_DEFINE = 47;
		public const int FUN_BODY = 48;
		public const int GREAT_EQUALS = 49;
		public const int GREAT_THAN = 50;
		public const int HEX_LITERATE = 51;
		public const int IDENTIFIER = 52;
		public const int IF_STATEMENT = 53;
		public const int IMPORT = 54;
		public const int INCREMENT = 55;
		public const int INSTANCEOF = 56;
		public const int INTERNAL = 57;
		public const int LAMBDA_FUNCTION = 58;
		public const int LBRACE = 59;
		public const int LCURLY = 60;
		public const int LESS_EQUALS = 61;
		public const int LESS_THAN = 62;
		public const int LINE_COMMENT = 63;
		public const int LIT_FALSE = 64;
		public const int LIT_NULL = 65;
		public const int LIT_TRUE = 66;
		public const int LOCAL_DECLARE_ASSIGNMENT = 67;
		public const int LOGICAL_AND = 68;
		public const int LOGICAL_OR = 69;
		public const int LPAREN = 70;
		public const int LSHIFT = 71;
		public const int MEMBER_DECLARATION = 72;
		public const int MEMBER_MODIFIER = 73;
		public const int MINUS = 74;
		public const int MOD = 75;
		public const int MUL = 76;
		public const int NAN = 77;
		public const int NEWLINE = 78;
		public const int NOT = 79;
		public const int NOT_EQUALS = 80;
		public const int NUMBER_LITERATE = 81;
		public const int OBJECT_LITERAL = 82;
		public const int OR = 83;
		public const int PARAMETER_DEFINES = 84;
		public const int PLUS = 85;
		public const int POST_UNARY_STEP = 86;
		public const int PRE_UNARY = 87;
		public const int PRE_UNARY_STEP = 88;
		public const int PRIVATE = 89;
		public const int PROPERTY_ACCESS = 90;
		public const int PROTECTED = 91;
		public const int PUBLIC = 92;
		public const int RANGE_LITERAL = 93;
		public const int RBRACE = 94;
		public const int RCURLY = 95;
		public const int RETURN = 96;
		public const int RPAREN = 97;
		public const int RSHIFT = 98;
		public const int SEMI = 99;
		public const int STRICT_EQUALS = 100;
		public const int STRICT_NOT_EQUALS = 101;
		public const int STRING_LITERATE = 102;
		public const int SWITCH = 103;
		public const int SWITCH_CASE = 104;
		public const int SWITCH_CASE_ELSE = 105;
		public const int TAG = 106;
		public const int TAG_ATTR = 107;
		public const int TAG_ATTR_LIST = 108;
		public const int TAG_NAME = 109;
		public const int TEMPLATE_DEFINE = 110;
		public const int TEMPLATE_TAG = 111;
		public const int THIS = 112;
		public const int TRY_CATCH = 113;
		public const int TRY_CATCH_CASE = 114;
		public const int TRY_CATCH_FINAL = 115;
		public const int TRY_CATCH_TRHOW = 116;
		public const int TYPE = 117;
		public const int TYPEOF = 118;
		public const int UNDEFINED = 119;
		public const int WHILE_STATEMENT = 120;
		public const int WS = 121;
		public const int XOR = 122;

		// Virtual node types (not produced by the lexer, only by the parser or runtime)
		public const int DEBUGGER = 150;

		// Bitwise NOT (~) — distinct from logical NOT (!)
		public const int BITWISE_NOT = 151;

		public const int MAX_TOKENS = 200;
		public const int REPLACED_TREE = MAX_TOKENS - 1;
	}
}
