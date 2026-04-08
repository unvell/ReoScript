grammar ReoScript;

options {
	output=AST;

	language = CSharp3;
	ASTLabelType=CommonTree;

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
	CONST_VALUE; RANGE_LITERAL; DEBUGGER;
}

@lexer::namespace {unvell.ReoScript}
@lexer::mebmers {private const int HIDDEN = Hidden;}
@lexer::modifier {sealed internal}

@parser::namespace {unvell.ReoScript}
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
		
		-> {DefineLocalFunction(id.Text, pl == null ? null : pl.Tree, body.Tree, 
		mod == null ? 0 : mod.Tree.Type, retval.Start.Line, retval.Start.CharPositionInLine)}

	;

anonymous_function_define
	: 'function' '(' pl=parameterDeclarationList? ')' b=functionBody
		-> {DefineAnonymousFunction(null, pl == null ? null : pl.Tree, b.Tree, 
		0, retval.Start.Line, retval.Start.CharPositionInLine)}

	| '(' pl=parameterDeclarationList? ')' '=>' (
		  b=functionBody
		  	-> {DefineAnonymousFunction(null, pl == null ? null : pl.Tree, b.Tree,
		  	0, retval.Start.Line, retval.Start.CharPositionInLine)}

		| exp=assignmentExpression
			-> {DefineAnonymousFunction(null, pl == null ? null : pl.Tree, 
   				(CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(RETURN, "RETURN"), exp.Tree),
   				0, retval.Start.Line, retval.Start.CharPositionInLine)}

		)
	| id=IDENTIFIER '=>' (
		  b=functionBody
		  	-> {DefineAnonymousFunction(id.Text, null, b.Tree,
		  	0, retval.Start.Line, retval.Start.CharPositionInLine)}

		| exp=assignmentExpression
			-> {DefineAnonymousFunction(id.Text, null, 
   				(CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(RETURN, "RETURN"), exp.Tree),
   				0, retval.Start.Line, retval.Start.CharPositionInLine)}

		)
	;

parameterDeclarationList
	: IDENTIFIER (COMMA! IDENTIFIER)*
	;
	
block
	: '{' statement* '}'
		-> ^(BLOCK statement*)
	;
	
functionBody
	:
	'{'
	{PushLocalStack();}
	 statement*
	{PopLocalStack();}
	'}'	
	-> ^(BLOCK statement*)
	;

/*	
class_define
	: 'class' IDENTIFIER (':' (IDENTIFIER ':')? IDENTIFIER)?
		'{'
			(
			  localMemberVariableDeclaration SEMI!
			| function_define SEMI!?
			)*
		'}'
	;
*/
	
statement
	: 
	  importStatement SEMI!
	| localVariableDeclaration SEMI!
	| function_define SEMI!?
//	| class_define SEMI!?
	| tagTemplateDefine SEMI!?
	| embeddedStatement
	;

importStatement
	: 'import' 
	 (
	   namespaceDeclare 	-> ^(IMPORT namespaceDeclare)
	 | STRING_LITERATE 		-> ^(IMPORT STRING_LITERATE)
	 )
	;

namespaceDeclare
	: IDENTIFIER ('.' (IDENTIFIER|'*'))*
	;

embeddedStatement
	: 
	  block
	| statementExpression SEMI!
	| ifelse
	| forStatement
	| foreachStatement
	| whileStatement
	| switchStatement
	| terminalStatement
	| tryCatchStatement
	| 'debugger' -> DEBUGGER
	;

statementExpression
	: 
	//  assignmentStatement |
	invocationExpression
	| 'new' primaryExpression
		-> ^(CREATE primaryExpression)
	| 'delete' primaryExpression
		-> ^(DELETE_PROP primaryExpression)	
	| '++' primaryExpression
		-> ^(PRE_UNARY_STEP primaryExpression '++')
	| '--' primaryExpression
		-> ^(PRE_UNARY_STEP primaryExpression '--')
	;

localMemberVariableDeclaration
	: memberModifier? TYPE localVariableDeclarationAssignment (',' localVariableDeclarationAssignment)*
		-> ^(MEMBER_DECLARATION ^(DECLARATION TYPE localVariableDeclarationAssignment+) ^(MEMBER_MODIFIER memberModifier?))
	;

localVariableDeclaration
	: TYPE localVariableDeclarationAssignment (',' localVariableDeclarationAssignment)*
		-> ^(DECLARATION TYPE localVariableDeclarationAssignment+)
	;

localVariableDeclarationAssignment
	: id=IDENTIFIER ('=' exp=expression)?
		-> { DefineLocalVariable(id.Text, exp == null ? null : exp.Tree, retval.Start.Line, retval.Start.CharPositionInLine)}
	;
	
memberModifier:
	PRIVATE | INTERNAL | PROTECTED | PUBLIC
	;

invocationExpression
	: 
	  id=primaryExpression
	  (
	  	'=' expression		-> ^(ASSIGNMENT $id expression)
	  |	'+=' expression		-> ^(ASSIGNMENT $id ^(PLUS $id expression))
	  |	'-=' expression		-> ^(ASSIGNMENT $id ^(MINUS $id expression))
	  |	'*=' expression		-> ^(ASSIGNMENT $id ^(MUL $id expression))
	  |	'/=' expression		-> ^(ASSIGNMENT $id ^(DIV $id expression))
	  |	'%=' expression		-> ^(ASSIGNMENT $id ^(MOD $id expression))
	  |	'&=' expression		-> ^(ASSIGNMENT $id ^(AND $id expression))
	  |	'|=' expression		-> ^(ASSIGNMENT $id ^(OR $id expression))
	  |	'^=' expression		-> ^(ASSIGNMENT $id ^(XOR $id expression))
	  |	'<<=' expression	-> ^(ASSIGNMENT $id ^(LSHIFT $id expression))
	  |	'>>=' expression	-> ^(ASSIGNMENT $id ^(RSHIFT $id expression))
	  | '++'				-> ^(POST_UNARY_STEP $id '++')
	  | '--'				-> ^(POST_UNARY_STEP $id '--')
	  |						-> primaryExpression
	  )
	;

statementExpressionList
	: statementExpression (COMMA! statementExpression)*
	;

public
expression
	: 
	  ( tag 
	  | assignmentExpression ) EOF!?
	;

range_literal
	: from=IDENTIFIER ':' to=IDENTIFIER -> ^(RANGE_LITERAL $from $to)
	;
	
expressionList
	: expression (COMMA! expression)*
	;
	
assignmentExpression
	: conditionalExpression (ASSIGNMENT^ expression)?
	;
	
conditionalExpression
	: conditionalOrExpression (CONDITION^ expression COLON! expression)?
	;

conditionalOrExpression
	: conditionalAndExpression (LOGICAL_OR^ conditionalAndExpression)*
	;

conditionalAndExpression
	: inclusiveOrExpression (LOGICAL_AND^ inclusiveOrExpression)*
	;
	
inclusiveOrExpression 
	: exclusiveOrExpression (OR^ exclusiveOrExpression)*
	;
	
exclusiveOrExpression
	: andExpression (XOR^ andExpression)*
	;
	
andExpression
	: instanceOfExpression (AND^ instanceOfExpression)*
	;
	
instanceOfExpression
	: equalityExpression ('instanceof'^ expression)?
	;
	
equalityExpression
	: relationalExpression ((EQUALS | NOT_EQUALS | STRICT_EQUALS | STRICT_NOT_EQUALS)^ relationalExpression)*
	;
	
relationalExpression // is as 
	: shiftExpression ((GREAT_EQUALS | GREAT_THAN | LESS_EQUALS | LESS_THAN)^ shiftExpression)*
	;
	
shiftExpression
	: additiveExpression ((LSHIFT|RSHIFT)^ additiveExpression)*
	;
	
additiveExpression 
	: multiplicativeExpression ((PLUS|MINUS)^ multiplicativeExpression)*
	;
	
multiplicativeExpression
	: unaryExpression ((MUL | DIV | MOD)^ unaryExpression)* 
	;
	
unaryExpression
	: 
	  primaryExpression ( 
	  		'++'  -> ^(POST_UNARY_STEP primaryExpression '++')
	      | '--'  -> ^(POST_UNARY_STEP primaryExpression '--')
	      | -> primaryExpression
	      )
	| '++' primaryExpression
		-> ^(PRE_UNARY_STEP primaryExpression '++')
	| '--' primaryExpression
		-> ^(PRE_UNARY_STEP primaryExpression '--')
	| 'new' primaryExpression
		-> ^(CREATE primaryExpression)
	| (op='+'|op='-'|op='!'|op='~') unaryExpression
		-> ^(PRE_UNARY $op unaryExpression)
    | 'typeof' unaryExpression -> ^(TYPEOF unaryExpression)
	;

primaryExpression
	: 
	(
		  variable 						-> variable
		| literal 						-> literal
		| cl=constLiteral 				-> ^(CONST_VALUE { ConstLiteral(cl.Tree) }) 
		| array_literal					-> array_literal
		| anonymous_function_define		-> anonymous_function_define
		| '(' expression ')'			-> expression
	)
	(
		'(' ( 
			')'		-> ^(FUNCTION_CALL $primaryExpression)
			| exp=expressionList ')'
					-> ^(FUNCTION_CALL $primaryExpression ^(ARGUMENT_LIST $exp))
			)
		| '.' IDENTIFIER
			-> ^(PROPERTY_ACCESS $primaryExpression IDENTIFIER)
		| '[' idx=expression ']'
			-> ^(ARRAY_ACCESS $primaryExpression $idx)
		| object_literal
			-> ^(COMBINE_OBJECT $primaryExpression object_literal)
	)*
	| (
	object_literal				-> object_literal
	)
	( '.' IDENTIFIER
		-> ^(PROPERTY_ACCESS $primaryExpression IDENTIFIER)
	)*
	;

tag
	: '<' (ns=IDENTIFIER ':')? name=IDENTIFIER tagAttr* 
	(
	  '>' s=tagStmt  '</' (IDENTIFIER ':')? IDENTIFIER '>' 
	| '/>' 
	)
	-> ^(TAG ^(TAG_NAME $name $ns?) ^(TAG_ATTR_LIST tagAttr*) $s? ) 
	;

tagStmt
	: (statement | tag)*
	;

tagAttr
	: name=IDENTIFIER '=' val=unaryExpression
		-> ^(TAG_ATTR $name $val)
	;
	
tagTemplateDefine
	: memberModifier? 'template' '<' typename=IDENTIFIER '>' ( '(' args=parameterDeclarationList? ')' )? tag
		-> ^(TEMPLATE_DEFINE $typename ^(PARAMETER_DEFINES $args?) tag)
	;

variable
	: IDENTIFIER 
	;

array_literal
	:	'[' expressionList? ','* ']'
			-> ^(ARRAY_LITERAL expressionList?)
	;

object_literal
	:	'{' keypair? (',' keypair)* ','* '}'
			-> ^(OBJECT_LITERAL keypair*)
	;
	
keypair
	:	(variable|STRING_LITERATE) ':'! expression 
	;
	
public
jsonParse[ScriptContext ctx, System.Action<string, object> handler]
	:	'{' jsonParse_keypair[ctx, handler]? (',' jsonParse_keypair[ctx, handler])* ','* '}'
	;

jsonParse_keypair[ScriptContext ctx, System.Action<string, object> handler]
	:	(var=variable|id=STRING_LITERATE) ':'! exp=expression {
		handler(var == null ? id.Text : var.Tree.Text, ScriptRunningMachine.ParseNode(exp.Tree, ctx));
	}
	;

/********************** control statements **********************/
	
ifelse
	: 'if' LPAREN conditionalOrExpression RPAREN es1=embeddedStatement ('else' es2=embeddedStatement)? 
		-> ^(IF_STATEMENT conditionalOrExpression $es1 $es2? )
	;
  
forStatement
	: 'for' '(' forInit? SEMI conditionalOrExpression? SEMI statementExpressionList? ')' embeddedStatement
		->	^(FOR_STATEMENT 
				^(FOR_INIT forInit?) 
				^(FOR_CONDITION conditionalOrExpression?) 
				^(FOR_ITERATOR statementExpressionList?)
				^(FOR_BODY embeddedStatement)
			  )
	;
	
forInit
	: localVariableDeclaration 
	| statementExpressionList
	;
	
foreachStatement
	: 'for' '(' local='var'? IDENTIFIER 'in' expression ')' embeddedStatement
		-> ^(FOREACH_STATEMENT IDENTIFIER expression embeddedStatement $local?)
	;

whileStatement
	: 'while' LPAREN (conditionalOrExpression) RPAREN embeddedStatement
		-> 	^(FOR_STATEMENT 
				^(FOR_INIT) 
				^(FOR_CONDITION conditionalOrExpression?) 
				^(FOR_ITERATOR)
				^(FOR_BODY embeddedStatement)
			  )
	;

switchStatement
	: 'switch' '(' conditionalOrExpression ')'
	  '{' switchCaseStatementList? '}'
	  -> ^(SWITCH conditionalOrExpression switchCaseStatementList?)
	;

switchCaseStatementList
	: (switchCaseCondition)+
	;
	
switchCaseCondition
	: 
	  'case' expression ':' 		-> ^(SWITCH_CASE expression)
	| statement 					-> statement
	| 'default' ':' 				-> ^(SWITCH_CASE_ELSE)
	;
	
tryCatchStatement
	: 'try' t=block ( ('catch' ('(' err=IDENTIFIER ')')?  b=block) | ('finally' f=block) )
		-> ^(TRY_CATCH $t ^(TRY_CATCH_CASE $b? $err?) ^(TRY_CATCH_FINAL $f?))
	| 'throw' expression SEMI
		-> ^(TRY_CATCH_TRHOW expression)
	;
	
terminalStatement
	: ( returnStatement -> returnStatement | ('break')->BREAK | ('continue')->CONTINUE ) SEMI
	;

returnStatement
	: 'return' expression? -> ^(RETURN expression?)
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

