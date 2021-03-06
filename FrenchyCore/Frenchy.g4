grammar Frenchy;

program: line* EOF;

line: statement | ifBlock | whileBlock | forBlock | foreachBlock;

statement: (assignment|functionCall) ';';

ifBlock: 'si ('expression')' block ('sinon' elseIfBlock)?;

elseIfBlock: block | ifBlock; 

whileBlock: WHILE '('expression')' block; 

WHILE: 'pendant que' | 'jusque';

forBlock: 'pour ('assignmentTemp ',' expression ',' assignmentTemp ')' block;

foreachBlock: 'pour chaque ('assignmentTempForeach ', dans' IDENTIFIER')' block;

assignment: IDENTIFIER '=>' expression;
assignmentTemp: IDENTIFIER '=>' expression;
assignmentTempForeach: dataTypes ',' IDENTIFIER;

functionCall: IDENTIFIER '(' (expression (',' expression)*)? ')';
function: 'fonction' IDENTIFIER '(' funcArgs ')' block;
funcArgs: (IDENTIFIER (',' IDENTIFIER)*)?;

expression
	: constant								#constantExpression
	| list									#listExpression
	| IDENTIFIER							#identifierExpression
	| functionCall							#functionCallExpression
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
boolOp: 'et' | 'ou' | 'et/ou';

list: 'Liste (' dataTypes ')' listDatas? ;
listDatas: constant (',' constant)*;

dataTypes: 'INTEGER' | 'FLOAT' | 'STRING' | 'BOOL';

constant: INTEGER | FLOAT | STRING | BOOL | NULL;
INTEGER: [0-9]+;
FLOAT: [0-9]+ '.' [0-9]+;
STRING: ('"' ~'"'* '"') | ('\'' ~'\''* '\'');
BOOL: 'vrai' | 'faux';
NULL: 'VIDE';

block: '{' line* '}';

WS: [\t\r\n]+ -> skip; 
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;

SIMPLE_COMMENT: '//' [a-zA-Z0-9_]* ~[\r\n]* -> skip;