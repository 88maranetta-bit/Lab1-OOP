grammar Formula;

compileUnit : expr EOF ;

expr
    : expr op=('*'|'/') expr # MulDiv
    | expr op=('+'|'-') expr # AddSub
    | NUMBER                 # Number
    | CELLREF                # CellRef
    | '(' expr ')'           # Parentheses
    ;

NUMBER : [0-9]+ ('.' [0-9]+)? ;
CELLREF : [A-Z] [0-9]+ ;
WS : [ \t\r\n]+ -> skip ;