namespace Ergo.Solver.BuiltIns;

public sealed class Unify : SolverBuiltIn
{
    public Unify()
        : base("", new("unify"), Maybe<int>.Some(2), WellKnown.Modules.Prologue)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        if (LanguageExtensions.Unify(arguments[0], arguments[1]).TryGetValue(out var subs))
        {
            yield return new(WellKnown.Literals.True, subs);
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
