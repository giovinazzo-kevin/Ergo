using Ergo.Lang;
using Ergo.Shell;


var shell = new ErgoShell(interpreter =>
{
    // interpreter.TryAddDirective lets you extend the interpreter
    // interpreter.AddDataSource lets you work on C# enumerables by implicitly handling all marshalling (TODO: AddDataSink)
    interpreter.AddDataSource(new[]
    {
        new Line(new(0, 0), new(10, 10)),
        new Line(new(3, 5), new(12, 6)),
        new Line(new(-1, 1), new(1, -1)),
        new()
    });
    interpreter.AddDataSource(new[]
    {
        new Polygon(new[]{new Line(new(3, 5), new(12, 6)),
        new Line(new(-1, 1), new(1, -1)),
        new() })
    });
}, solver =>
{
    // solver.TryAddBuiltIn lets you write built-in predicates in C#
});
// shell.TryAddCommand lets you extend the shell

var scope = shell.CreateScope();
shell.EnterRepl(ref scope);


[Term(Functor = ",", Marshalling = TermMarshalling.Positional)]
public readonly record struct Point(int X, int Y);
[Term(Functor = "-", Marshalling = TermMarshalling.Positional)]
public readonly record struct Line([property: Term(Functor = "s", Marshalling = TermMarshalling.Positional)] Point Start, Point End);
[Term(Functor = "polygon", Marshalling = TermMarshalling.Positional)]
public readonly record struct Polygon([property:Term(Functor = "l", Marshalling = TermMarshalling.Named)] Line[] Segments);
