
namespace Ergo.Solver.BuiltIns;

public sealed class Not : SolverBuiltIn
{
    public Not()
        : base("", new("not"), Maybe<int>.Some(1), WellKnown.Modules.Prologue)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var solutions = context.Solver.Solve(new Query(arguments.Single()), scope);
        if (solutions.Any())
        {
            yield return False();
        }
        else
        {
            yield return True();
        }
    }
}
