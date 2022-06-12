namespace Ergo.Solver.BuiltIns;

public sealed class AssertZ : DynamicPredicateBuiltIn
{
    public AssertZ()
        : base("", new("assertz"), Maybe.Some(1))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        if (Assert(solver, arguments[0], z: true))
        {
            yield return new(WellKnown.Literals.True);
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
