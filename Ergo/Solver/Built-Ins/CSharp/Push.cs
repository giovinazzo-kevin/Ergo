

namespace Ergo.Solver.BuiltIns;

public sealed class Push : SolverBuiltIn
{
    public Push()
        : base("", new("push_data"), Maybe<int>.Some(1), WellKnown.Modules.CSharp)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (!args[0].IsGround)
        {
            scope.Throw(SolverError.TermNotSufficientlyInstantiated, args[0].Explain(true));
            yield return new(WellKnown.Literals.False);
            yield break;
        }

        solver.PushData(args[0]);
        yield return new(WellKnown.Literals.True);
    }
}
