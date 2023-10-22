namespace Ergo.Solver.BuiltIns;

public sealed class Unify : SolverBuiltIn
{
    public Unify()
        : base("", new("unify"), Maybe<int>.Some(2), WellKnown.Modules.Prologue)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        if (arguments[0].Unify(arguments[1]).TryGetValue(out var subs))
            yield return True(subs);
        else
            yield return False();
    }
}
