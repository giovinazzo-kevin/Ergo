namespace Ergo.Solver.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(1))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var eval = scope.InterpreterScope.ExceptionHandler.TryGet(() => new Evaluation(new Atom(Evaluate(solver, scope, arguments[0]))));
        yield return eval.GetOr(False());
    }
}
