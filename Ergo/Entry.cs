using Ergo.Shell;

var shell = new ErgoShell(interpreter =>
{
    // interpreter.TryAddDirective lets you extend the interpreter
}, solver =>
{
    // solver.TryAddBuiltIn lets you write built-in predicates in C#
});
// shell.TryAddCommand lets you extend the shell

var scope = shell.CreateScope();
shell.EnterRepl(ref scope);
