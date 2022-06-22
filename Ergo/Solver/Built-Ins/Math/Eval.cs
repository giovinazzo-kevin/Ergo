namespace Ergo.Solver.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(1))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var eval = scope.InterpreterScope.ExceptionHandler.TryGet(() => new Evaluation(new Atom(Evaluate(solver, arguments[0], solver.InterpreterScope))));
        if (eval.HasValue)
        {
            yield return eval.GetOrThrow();
            yield break;
        }

        yield return new Evaluation(WellKnown.Literals.False);
    }
}
