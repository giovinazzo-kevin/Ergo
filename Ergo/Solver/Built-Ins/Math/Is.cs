namespace Ergo.Solver.BuiltIns;

public sealed class Is : MathBuiltIn
{
    public Is()
        : base("", new("is"), Maybe<int>.Some(2))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var result = new Atom(Evaluate(solver, scope, arguments[1]));
        if (arguments[0].Unify(result).TryGetValue(out var subs))
        {
            yield return new(WellKnown.Literals.True, subs.ToArray());
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
