using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Not : BuiltIn
{
    public Not()
        : base("", new("not"), Maybe<int>.Some(1), WellKnown.Modules.Prologue)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var solutions = await solver.Solve(new Query(arguments.Single()), Maybe.Some(scope)).CollectAsync();
        if (solutions.Any())
        {
            yield return new(WellKnown.Literals.False);
        }
        else
        {
            yield return new(WellKnown.Literals.True);
        }
    }
}
