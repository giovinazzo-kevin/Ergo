using Ergo.Facade;


var x = new Complex(new Atom("pred"), new Variable("A"), new Complex(new Atom("f"), new Atom("1"), new Atom("2")))
    .ToTermExpression();
var y = new Complex(new Atom("pred"), new Atom("3"), new Variable("B"))
    .ToTermExpression();

var z = x.Unify(y).GetOrThrow();
var x1 = z.Context.BacktrackTo.ToTerm();


var facade = ErgoFacade.Standard;
var shell = facade.BuildShell();

await foreach (var _ in shell.Repl())
    ;
