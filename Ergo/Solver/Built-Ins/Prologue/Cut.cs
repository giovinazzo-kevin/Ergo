namespace Ergo.Solver.BuiltIns;

public sealed class Cut : SolverBuiltIn
{
    public Cut()
        : base("", new("!"), Maybe<int>.Some(0), WellKnown.Modules.Prologue)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        yield return True();
    }
}
