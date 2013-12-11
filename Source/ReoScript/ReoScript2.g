grammar ReoScript2;

options {
	//output=AST;

	language = CSharp3;
	//ASTLabelType=CommonTree;

	//language = C;
	//ASTLabelType = pANTLR3_BASE_TREE;

	//k = 2;  	
  	//backtrack = true; 
   	//memoize = true;
   	//analyzerDebug = false;
   	//codeGenDebug = false;
}

tokens {
	PLUS 				= '+'	;
	MINUS 				= '-'	;
	MUL					= '*'	;
	DIV					= '/'	;
	MOD					= '%'	;
	AND					= '&'	;
	OR					= '|'	;
	XOR					= '^'	;
	GREAT_EQUALS		= '>='	;
	GREAT_THAN			= '>'	;
	LESS_EQUALS			= '<='	;
	LESS_THAN			= '<'	;
	LSHIFT				= '<<'	;
	RSHIFT				= '>>'	;
	LOGICAL_AND			= '&&'	;
	LOGICAL_OR			= '||'	;
	EQUALS				= '=='	;
	NOT_EQUALS			= '!='	;
	STRICT_EQUALS		= '===' ;
	STRICT_NOT_EQUALS 	= '!==' ;
	
	IMPORT;
	
	FUNCTION_DEFINE; ANONYMOUS_FUNCTION; LAMBDA_FUNCTION; PARAMETER_DEFINES; FUN_BODY;

	DECLARATION; ASSIGNMENT; ARGUMENT_LIST; LOCAL_DECLARE_ASSIGNMENT; MEMBER_MODIFIER;
	PRE_UNARY_STEP; POST_UNARY_STEP; PRE_UNARY; 
		
	FOR_INIT; FOR_CONDITION; FOR_STATEMENT; FOR_ITERATOR; FOR_BODY; 
	WHILE_STATEMENT; FOREACH_STATEMENT; 
	IF_STATEMENT; SWITCH; SWITCH_CASE; SWITCH_CASE_ELSE;
	TRY_CATCH; TRY_CATCH_CASE; TRY_CATCH_FINAL; TRY_CATCH_TRHOW;
	BLOCK; RETURN; CONDITION; BREAK; CONTINUE;
	
	PROPERTY_ACCESS; ARRAY_ACCESS; FUNCTION_CALL; 
	CREATE; DELETE_PROP; ARRAY_LITERAL; OBJECT_LITERAL; COMBINE_OBJECT; TYPEOF;
	
	TAG; TAG_ATTR_LIST; TAG_ATTR; TAG_NAME; TEMPLATE_DEFINE; TEMPLATE_TAG;
	CLASS; MEMBER_DECLARATION;
	
	// extends
	CONST_VALUE;
}

@lexer::namespace {Unvell.ReoScript}
@lexer::mebmers {private const int HIDDEN = Hidden;}
@lexer::modifier {sealed internal}

@parser::namespace {Unvell.ReoScript}
@parser::modifier {sealed internal}

public
script
	: 
	{PushLocalStack();}
	statement*
	{PopLocalStack();}
	;
	
function_defines
	: function_define*
	;

function_define
	: 
	mod=memberModifier? 'function' id=IDENTIFIER '(' pl=parameterDeclarationList? ')' body=functionBody
	
		  //DefineFunction(id, parameterDeclarationList, block);
	
		 {DefineLocalFunction(id.Text, pl == null ? null : pl.Tree, body.Tree, 
		mod == null ? 0 : mod.Tree.Type, retval.Start.Line, retval.Start.CharPositionInLine)}
		
//		-> ^(FUNCTION_DEFINE $id ^(PARAMETER_DEFINES parameterDeclarationList?) block 
//			^(MEMBER_MODIFIER memberModifier?) {currentStack})
	;

anonymous_function_define
	: 'function' '(' pl=parameterDeclarationList? ')' b=functionBody
		{DefineAnonymousFunction(null, pl == null ? null : pl.Tree, b.Tree, 
		0, retval.Start.Line, retval.Start.CharPositionInLine)}
	| '(' pl=parameterDeclarationList? ')' '=>' (
		  b=functionBody
		  	 {DefineAnonymousFunction(null, pl == null ? null : pl.Tree, b.Tree,
		  	0, retval.Start.Line, retval.Start.CharPositionInLine)}
		| exp=assignmentExpression
			 {DefineAnonymousFunction(null, pl == null ? null : pl.Tree, 
   				(CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(RETURN, "RETURN"), exp.Tree),
   				0, retval.Start.Line, retval.Start.CharPositionInLine)}
		)
	| id=IDENTIFIER '=>' (
		  b=functionBody
		  	 {DefineAnonymousFunction(id.Text, null, b.Tree,
		  	0, retval.Start.Line, retval.Start.CharPositionInLine)}
		| exp=assignmentExpression
			 {DefineAnonymousFunction(id.Text, null, 
   				(CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(RETURN, "RETURN"), exp.Tree),
   				0, retval.Start.Line, retval.Start.CharPositionInLine)}
		)
	;

parameterDeclarationList
	: IDENTIFIER (COMMA IDENTIFIER)*
	;
	
block
	: '{' statement* '}'
	;
	
functionBody
	:
	'{'
	{PushLocalStack();}
	 statement*
	{PopLocalStack();}
	'}'	

	;


statement
	: 
	  importStatement SEMI
	| localVariableDeclaration SEMI
	| function_define SEMI?
	| tagTemplateDefine SEMI?
	| embeddedStatement
	;

importStatement
	: 'import' 
	 (
	   namespaceDeclare 	
	 | STRING_LITERATE 		
	 )
	;

namespaceDeclare
	: IDENTIFIER ('.' (IDENTIFIER|'*'))*
	;

embeddedStatement
	: 
	  block
	| statementExpression SEMI
	| ifelse
	| forStatement
	| foreachStatement
	| whileStatement
	| switchStatement
	| terminalStatement
	| tryCatchStatement
	;

statementExpression
	: 
	invocationExpression
	| 'new' primaryExpression
	| 'delete' primaryExpression
	| '++' primaryExpression
	| '--' primaryExpression
	;

localMemberVariableDeclaration
	: memberModifier? TYPE localVariableDeclarationAssignment (',' localVariableDeclarationAssignment)*
	;

localVariableDeclaration
	: TYPE localVariableDeclarationAssignment (',' localVariableDeclarationAssignment)*
	;

localVariableDeclarationAssignment
	: id=IDENTIFIER ('=' exp=expression)?
	;
	
memberModifier:
	PRIVATE | INTERNAL | PROTECTED | PUBLIC
	;

invocationExpression
	: 
	  id=primaryExpression
	  (
	  	'=' expression		
	  |	'+=' expression		
	  |	'-=' expression		
	  |	'*=' expression		
	  |	'/=' expression		
	  |	'%=' expression		
	  |	'&=' expression		
	  |	'|=' expression		
	  |	'^=' expression		
	  |	'<<=' expression	
	  |	'>>=' expression	
	  | '++'				
	  | '--'				
	  |						
	  )
	;

statementExpressionList
	: statementExpression (COMMA statementExpression)*
	;

public
expression
	: 
//	'new' assignmentExpression
		//-> ^(CREATE assignmentExpression)
	//|
	  tag 
	| assignmentExpression EOF?
	;
	
expressionList
	: expression (COMMA expression)*
	;
	
assignmentExpression
	: conditionalExpression (ASSIGNMENT expression)?
	;
	
conditionalExpression
	: conditionalOrExpression (CONDITION expression COLON expression)?
	;

conditionalOrExpression
	: conditionalAndExpression (LOGICAL_OR conditionalAndExpression)*
	;

conditionalAndExpression
	: inclusiveOrExpression (LOGICAL_AND inclusiveOrExpression)*
	;
	
inclusiveOrExpression 
	: exclusiveOrExpression (OR exclusiveOrExpression)*
	;
	
exclusiveOrExpression
	: andExpression (XOR andExpression)*
	;
	
andExpression
	: instanceOfExpression (AND instanceOfExpression)*
	;
	
instanceOfExpression
	: equalityExpression ('instanceof' expression)?
	;
	
equalityExpression
	: relationalExpression ((EQUALS | NOT_EQUALS | STRICT_EQUALS | STRICT_NOT_EQUALS) relationalExpression)*
	;
	
relationalExpression // is as 
	: shiftExpression ((GREAT_EQUALS | GREAT_THAN | LESS_EQUALS | LESS_THAN) shiftExpression)*
	;
	
shiftExpression
	: additiveExpression ((LSHIFT|RSHIFT) additiveExpression)*
	;
	
additiveExpression 
	: multiplicativeExpression ((PLUS|MINUS) multiplicativeExpression)*
	;
	
multiplicativeExpression
	: unaryExpression ((MUL | DIV | MOD) unaryExpression)* 
	;
	
unaryExpression
	: 
	  primaryExpression ( '++' | '--' )
	| '++' primaryExpression
	| '--' primaryExpression
	| 'new' primaryExpression
	| (op='+'|op='-'|op='!'|op='~') unaryExpression
    | 'typeof' unaryExpression
	;

primaryExpression
	: 
	(
		  variable 				
		| literal 						
		| cl=constLiteral 				
		| array_literal					
		| anonymous_function_define		
		| '(' expression ')'			
	)
	(
		'(' expressionList? ')'	
		| '.' IDENTIFIER
		| '[' idx=expression ']'
		| object_literal
	)*
	| (
	object_literal		
	)
	( '.' IDENTIFIER
	)*
	;

tag
	: '<' (ns=IDENTIFIER ':')? name=IDENTIFIER tagAttr* 
	(
	  '>' s=tagStmt  '</' (IDENTIFIER ':')? IDENTIFIER '>' 
	| '/>' 
	)
	;

tagStmt
	: (statement | tag)*
	;

tagAttr
	: name=IDENTIFIER '=' val=unaryExpression
	;
	
tagTemplateDefine
	: memberModifier? 'template' '<' typename=IDENTIFIER '>' ( '(' args=parameterDeclarationList ')' )? tag
	;

variable
	: IDENTIFIER
	;

array_literal
	:	'[' expressionList? ','* ']'
	;

object_literal
	:	'{' keypair? (',' keypair)* ','* '}'
		
	;
	
keypair
	:	(variable|STRING_LITERATE) ':' expression 
	;
	
public
jsonParse[ScriptContext ctx, System.Action<string, object> handler]
	:	'{' jsonParse_keypair[ctx, handler]? (',' jsonParse_keypair[ctx, handler])* ','* '}'
	;

jsonParse_keypair[ScriptContext ctx, System.Action<string, object> handler]
	:	(var=variable|id=STRING_LITERATE) ':' exp=expression {
		handler(var == null ? id.Text : var.Tree.Text, ScriptRunningMachine.ParseNode(exp.Tree, ctx));
	}
	;

/********************** control statements **********************/
	
ifelse
	: 'if' LPAREN conditionalOrExpression RPAREN es1=embeddedStatement ('else' es2=embeddedStatement)? 
	;
  
forStatement
	: 'for' '(' forInit? SEMI conditionalOrExpression? SEMI statementExpressionList? ')' embeddedStatement
	;
	
forInit
	: localVariableDeclaration 
	| statementExpressionList
	;
	
foreachStatement
	: 'for' '(' local='var'? IDENTIFIER 'in' expression ')' embeddedStatement
	
	;

whileStatement
	: 'while' LPAREN (conditionalOrExpression) RPAREN embeddedStatement
	;

switchStatement
	: 'switch' '(' conditionalOrExpression ')'
	  '{' switchCaseStatementList? '}'
	;

switchCaseStatementList
	: (switchCaseCondition)+
	;
	
switchCaseCondition
	: 
	  'case' expression ':' 	
	| statement 				
	| 'default' ':' 			
	;
	
tryCatchStatement
	: 'try' t=block ( ('catch' ('(' err=IDENTIFIER ')')?  b=block) | ('finally' f=block) )
	| 'throw' expression SEMI
	;
	
terminalStatement
	: ( returnStatement | 'break' | 'continue') SEMI
	;

returnStatement
	: 'return' expression?
	;

/* construct statements */
/* literates */

literal
	:
	THIS
	;
	
constLiteral
	: LIT_TRUE
	| LIT_FALSE
	| NUMBER_LITERATE
	| STRING_LITERATE
	| LIT_NULL 
	| UNDEFINED
	| HEX_LITERATE
	| BINARY_LITERATE
	| NAN
	;

NUMBER_LITERATE
	: (('0'..'9')* '.')? ('0'..'9')+// -> ^(NUMBER_LITERATE literate+)
	;

HEX_LITERATE
	: '0' 'x' ('0'..'9'|'a'..'f'|'A'..'F')+
	;

BINARY_LITERATE
	: '0' 'b' ('0'|'1')+
	;

STRING_LITERATE
	:
	 '"' ~'"'* '"' //-> ^(STRING_LITERATE s)
	| '\'' ~'\''* '\''
	//  '"' ( ESCAPE_SEQUENCE | ~('\\'|'"') )* '"'
 	//| '\'' ( ESCAPE_SEQUENCE | ~('\\'|'\'') )* '\''
	;

fragment
ESCAPE_SEQUENCE 
    :   '\\' ('b'|'t'|'n'|'f'|'r'|'\"'|'\''|'\\')
    ;
	/*
simpleEscapeSequence
	: '\\\'' | '\\"' | '\\\\' | '\\0' | '\\a' | '\\b' | '\\f' | '\\n' | '\\r' | '\\t' | '\\v'
	;*/

/********************** end of literates **************************/
//PLUS			: '+'	;
//MINUS			: '-'	;

ASSIGNMENT		: '='	;
ASSIGN_PLUS		: '+='	;
ASSIGN_MINUS	: '-='	;
ASSIGN_MUL		: '*='	;
ASSIGN_DIV		: '/='	;
ASSIGN_REM		: '%='	;
ASSIGN_AND		: '&='	;
ASSIGN_OR		: '|='	;
ASSIGN_REV		: '^='	;
ASSIGN_LSHIFT	: '<<='	;
ASSIGN_RSHIFT	: '>>='	;

COMMA			: ','	;
LPAREN			: '('	;
RPAREN			: ')'	;
LBRACE			: '['	;
RBRACE			: ']'	;
LCURLY			: '{'	;
RCURLY			: '}'	;
COLON			: ':'	;
DOT				: '.'	;
NOT				: '!'	;

INCREMENT		: '++'	;
DECREMENT		: '--'	;

CONDITION		: '?'	;
ELSE			: 'else';

PRIVATE 		: 'private';
PROTECTED		: 'protected';
INTERNAL		: 'internal';
PUBLIC			: 'public';
TYPE			: 'var';
LIT_TRUE		: 'true';
LIT_FALSE		: 'false';
THIS			: 'this';
LIT_NULL		: 'null';
UNDEFINED 		: 'undefined';
NAN				: 'NaN';
INSTANCEOF		: 'instanceof';
			
COMMENT			: '/*' (options {greedy=false;} : .)* '*/' { $channel=HIDDEN; } ;
LINE_COMMENT	: '//' ~('\r'|'\n')* { $channel=HIDDEN; } ;

IDENTIFIER 		: ('a'..'z'|'A'..'Z'|'_'|'$') ('0'..'9'|'a'..'z'|'A'..'Z'|'_')* ;
NEWLINE			: '\r'? '\n' { $channel=HIDDEN; };
WS				: (' '|'\t' | NEWLINE )+ { $channel=HIDDEN; } ;
SEMI			: ';' ;

