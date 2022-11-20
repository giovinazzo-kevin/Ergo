namespace Ergo.Solver.BuiltIns;

public sealed class SetupCallCleanup : SolverBuiltIn
{
    public SetupCallCleanup()
        : base("", new("setup_call_cleanup"), 3, WellKnown.Modules.Meta)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var once = new Call().Apply(context, scope, new[] { args[0] }).FirstOrDefault();
        if (once.Equals(default(Evaluation)) || once.Result.Equals(WellKnown.Literals.False))
        {
            yield return new(WellKnown.Literals.False);
            yield break;
        }

        var any = false;
        foreach (var sol in new Call().Apply(context, scope, new[] { args[1] }))
        {
            if (sol.Result.Equals(WellKnown.Literals.False))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, sol.Substitutions);
            any = true;
        }

        new Call().Apply(context, scope, new[] { args[2] }).FirstOrDefault();
        if (!any)
        {
            yield return new(WellKnown.Literals.False);
            yield break;
        }
    }
}
