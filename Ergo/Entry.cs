using Ergo.Lang;
using Ergo.Interpreter;
using Ergo.Shell;

var shell = new ErgoShell(interpreter =>
{
    // interpreter.TryAddDirective
}, solver =>
{
    // solver.TryAddBuiltIn
});
// shell.TryAddCommand

var scope = shell.CreateScope();
shell.EnterRepl(ref scope);
