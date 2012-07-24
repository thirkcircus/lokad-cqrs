grammar MessageContracts;

options 
{
	language = 'CSharp2'; 
	output=AST; 
}

tokens 
{
	TypeToken;	
	CommandToken;
	EventToken;	
	MemberToken;
	BlockToken;
	DisctionaryToken;
	FragmentGroup;
	FragmentEntry;
	FragmentReference;
	ModifierDefinition;
	EntityDefinition;
	StringRepresentationToken;
}

@lexer::namespace { MessageContracts }
@parser::namespace { MessageContracts }

program	
	:	declaration+
	;
	
declaration
	: modifier_declaration
	| frag_declaration
	| type_declaration
	| entity_declaration
	;

frag_declaration 
	: LET ID '=' ID ID ';' -> ^(FragmentEntry ID ID ID);  
    
modifier_declaration
	: USING Modifier '=' ID ';' -> ^(ModifierDefinition Modifier ID);
    
	
entity_declaration
	: ENTITY ID block ';' -> ^(EntityDefinition ID block);
	
type_declaration
	: ID Modifier? block -> ^(TypeToken ID block Modifier?);
	
member 	
	:	ID ID -> ^(MemberToken ID ID)
	|	ID -> ^(FragmentReference ID)
	;

	
block
    :   lc='('
            (member (',' member)*)?
        ')' representation?
        -> ^(BlockToken[$lc,"Block"] member* representation?)
    ;    
    
representation
	:	AS STRING -> ^(StringRepresentationToken STRING);
AS	:	'as';
USING
	: 'using';
LET
	: 'let';	
ENTITY 	:	'entity';
ID  :	('a'..'z'|'A'..'Z'|'_')('a'..'z'|'A'..'Z'|'0'..'9'|'_'|'<'|'>'|'['|']')* ;


Modifier
	: '?'
	| '!'
	| ';'
	;


INT :	'0'..'9'+;   


STRING
    :  '"' ( ESC_SEQ | ~('\\'|'"') )* '"'
    ;


fragment
HEX_DIGIT : ('0'..'9'|'a'..'f'|'A'..'F') ;

fragment
ESC_SEQ
    :   '\\' ('b'|'t'|'n'|'f'|'r'|'\"'|'\''|'\\')
    |   UNICODE_ESC
    |   OCTAL_ESC
    ;

fragment
OCTAL_ESC
    :   '\\' ('0'..'3') ('0'..'7') ('0'..'7')
    |   '\\' ('0'..'7') ('0'..'7')
    |   '\\' ('0'..'7')
    ;

fragment
UNICODE_ESC
    :   '\\' 'u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT
    ;

COMMENT
    :   '//' ~('\n'|'\r')* '\r'? '\n' {$channel=HIDDEN;}
    |   '/*' ( options {greedy=false;} : . )* '*/' {$channel=HIDDEN;}
    ;

WS  :   ( ' '
        | '\t'
        | '\r'
        | '\n'
        ) {$channel=HIDDEN;}
    ;  