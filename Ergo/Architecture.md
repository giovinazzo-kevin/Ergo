# Architecture
```
         			 Shell     <- [Commands]  , Shell Scope
         			   ∧							∧
         			   |							|
Lexer -> Parser -> Interpreter <- [Directives], Interpreter Scope
             		   |                            |
             		   *----------------------------*
                       |
                       ∨
             		 Solver    <- [BuiltIns]  , Solver Scope
```

The shell is the top-level interactive interface *for* the interpreter. It provides easy access to I/O and parsing, as well as debugging utilities.
The interpreter is the fulcrum of the architecture. It is necessary in order to create a solver, but it can be used without a shell.
The solver is the engine that's actually responsible for answering queries. It is short-lived compared to the interpreter, as it's scoped to the Interpreter Scope.

All three layers of the architecture can be extended by implementing the corresponding Command, Directive or BuiltIn.

## Shell
Shell commands are expressed in terms of regular expressions. Whenever a command matches, its handler is executed.

## Interpreter
Interpreter directives are expressed in terms of predicate signatures. They receive a fixed or variadic number of ITerm arguments depending on their arity, and they execute once on module load.

##Solver
BuiltIns are also expressed in terms of predicate signatures. Unlike directives, they are resolved recursively right before a knowledge base is queried.