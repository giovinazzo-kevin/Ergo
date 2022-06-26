namespace Ergo.Solver.BuiltIns;

public sealed class Is : MathBuiltIn
{
    public Is()
        : base("", new("is"), Maybe<int>.Some(2))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var eval = scope.InterpreterScope.ExceptionHandler.TryGet(() => new Atom(Evaluate(solver, scope, arguments[1])));
        if (eval.TryGetValue(out var result) && arguments[0].Unify(result).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }

        yield return False();
    }
}
