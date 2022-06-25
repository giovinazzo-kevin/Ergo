namespace Ergo.Solver.BuiltIns;

public sealed class AssertA : DynamicPredicateBuiltIn
{
    public AssertA()
        : base("", new("asserta"), Maybe.Some(1))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        if (Assert(solver, scope, arguments[0], z: false))
        {
            yield return new(WellKnown.Literals.True);
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
