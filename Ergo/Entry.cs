using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Shell;


var shell = new ErgoShell(interpreter =>
{
    // interpreter.TryAddDirective lets you extend the interpreter
    // interpreter.TryAddDynamicPredicate lets you add native Ergo predicates to the interpreter programmatically
    // interpreter.AddDataSource automatically handles marshalling from C# to Ergo objects, and creates helpful dynamic predicate stubs for debugging
    interpreter.AddDataSource(new[]
    {
        new X(), new X(), new X()
    }, Maybe.Some(new Atom("y")));
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
// shell.TryAddCommand lets you extend the command shell

var scope = shell.CreateScope();
shell.EnterRepl(ref scope);


[Term(Marshalling = TermMarshalling.Positional)]
public readonly record struct X();
[Term(Functor = ",", Marshalling = TermMarshalling.Positional)]
public readonly record struct Point(int X, int Y);
[Term(Functor = "-", Marshalling = TermMarshalling.Positional)]
public readonly record struct Line([property: Term(Functor = "s", Marshalling = TermMarshalling.Positional)] Point Start, Point End);
[Term(Functor = "polygon", Marshalling = TermMarshalling.Positional)]
public readonly record struct Polygon([property:Term(Functor = "l", Marshalling = TermMarshalling.Named)] Line[] Segments);
