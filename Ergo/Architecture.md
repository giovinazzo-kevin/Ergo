# Architecture
///
         			 Shell     <- [Commands]  , Shell Scope
         			   ∧							∧
         			   |							|
Lexer -> Parser -> Interpreter <- [Directives], Interpreter Scope
             		   |                            |
             		   *----------------------------*
                       |
                       ∨
             		 Solver    <- [BuiltIns]  , Solver Scope
///

The shell is the top-level interactive interface *for* the interpreter. It provides easy access to I/O and parsing, as well as debugging utilities.
The interpreter is the fulcrum of the architecture. It is necessary in order to create a solver, but it can be used without a shell.
The solver is the engine that's actually responsible for answering queries. It is short-lived compared to the interpreter, as it's scoped to the Interpreter Scope.

All three layers of the architecture can be extended by implementing the corresponding Command, Directive or BuiltIn.

## Shell

## Interpreter

##Solver