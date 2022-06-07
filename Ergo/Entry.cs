using Ergo.Lang;
using Ergo.Shell;


var shell = new ErgoShell(interpreter =>
{
    // interpreter.TryAddDirective lets you extend the interpreter
    interpreter.AddDataSource(new[]
    {
        new Line(new(0, 0), new(10, 10)),
        new Line(new(3, 5), new(12, 6)),
        new Line(new(-1, 1), new(1, -1)),
        new()
    });
}, solver =>
{
    // solver.TryAddBuiltIn lets you write built-in predicates in C#
});
// shell.TryAddCommand lets you extend the shell

var scope = shell.CreateScope();
shell.EnterRepl(ref scope);


[Term(Marshalling = TermMarshalling.Positional)]
public readonly record struct Point(int X, int Y);
[Term(Marshalling = TermMarshalling.Positional)]
public readonly record struct Line(Point Start, Point End);
