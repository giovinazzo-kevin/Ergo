# Architecture
```
			 Shell    -> Commands   / Shell Scope
			   ^
			   |
Parser -> Interpreter -> Directives / Interpreter Scope
  ^			   ^
  |			   |
Lexer		 Solver   -> Built-ins  / Solver Scope
```