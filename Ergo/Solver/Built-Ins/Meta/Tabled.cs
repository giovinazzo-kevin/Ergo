namespace Ergo.Solver.BuiltIns;

public sealed class Tabled : SolutionAggregationBuiltIn
{
    public Tabled()
        : base("(called by tabled predicates implicitly)", new("tabled"), 2, WellKnown.Modules.Meta)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {

        yield return False();
    }
}
