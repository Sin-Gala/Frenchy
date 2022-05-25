grammar Frenchy;

program: line* EOF;

line: statement | ifBlock | whileBlock | forBlock | foreachBlock;

statement: (assignment|functionCall) ';';

ifBlock: 'si ('expression')' block ('autrement' elseIfBlock)?;

elseIfBlock: block | ifBlock; 

whileBlock: WHILE '('expression')' block; 

WHILE: 'pendant' | 'jusque';

forBlock: 'pour ('expression')' block;

foreachBlock: 'pour chaque ('IDENTIFIER ',' STRING ', dans' constant ')';

assignment: IDENTIFIER '=>' expression;

functionCall: IDENTIFIER '(' (expression (',' expression)*)? ')';

expression
	: constant								#constantExpression
	| IDENTIFIER							#identifierExpression
	| '(' expression ')'					#parenthesizedExpression
	| '!' expression						#notExpression
	| expression multOp expression			#multiplicativeExpression
	| expression addOp expression			#additiveExpression
	| expression compareOp expression		#comparisonExpression
	| expression boolOp expression			#booleanExpression
	;

multOp: '*' | '/' | '%';
addOp: '+' | '-';
compareOp: '==' | '!=' | '>' | '<' | '>=' | '<=';
boolOp: BOOL_OPERATOR;

BOOL_OPERATOR: 'et' | 'ou' | 'et/ou';

constant: INTEGER | FLOAT | STRING | BOOL | NULL;
INTEGER: [0-9]+;
FLOAT: [0-9]+ '.' [0-9]+;
STRING: ('"' ~'"'* '"') | ('\'' ~'\''* '\'');
BOOL: 'vrai' | 'faux';
NULL: 'VIDE';

block: '{' line* '}';

WS: [ \t\r\n]+ -> skip; 
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;