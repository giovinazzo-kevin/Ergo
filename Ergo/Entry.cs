using Ergo.CSharp;
using Ergo.Lang;
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


[Term(Marshalling = TermMarshalling.Positional, Module = "geometry")]
public readonly record struct Point(int X, int Y);
[Term(Marshalling = TermMarshalling.Positional, Module = "geometry")]
public readonly record struct Line(Point Start, Point End);
