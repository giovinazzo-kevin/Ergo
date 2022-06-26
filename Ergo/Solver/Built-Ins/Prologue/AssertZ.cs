namespace Ergo.Solver.BuiltIns;

public sealed class AssertZ : DynamicPredicateBuiltIn
{
    public AssertZ()
        : base("", new("assertz"), 1)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        if (Assert(solver, scope, arguments[0], z: true))
        {
            yield return new(WellKnown.Literals.True);
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
