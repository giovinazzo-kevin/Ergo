using Ergo.Lang;
var shell = new Shell();
shell.Load("stdlib/prologue.ergo");
shell.Parse(@"
'/'(X, Y) :- @ground(X), @ground(Y).
'//'(X, Y) :- (X / Y) ; (Y / X).

god / immortal :- true.
zeus / god :- true.

therefore(A/B, C/A, C/B) :- A//B, C/A.

");
shell.EnterRepl();