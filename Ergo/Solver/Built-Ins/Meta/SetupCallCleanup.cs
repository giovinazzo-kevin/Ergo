namespace Ergo.Solver.BuiltIns;

public sealed class SetupCallCleanup : SolverBuiltIn
{
    public SetupCallCleanup()
        : base("", new("setup_call_cleanup"), 3, WellKnown.Modules.Meta)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var once = new Call().Apply(context, scope, new[] { args[0] }).Select(x => Maybe.Some(x)).FirstOrDefault();
        if (!once.TryGetValue(out var sol) || !sol.Result)
        {
            yield return False();
            yield break;
        }

        var any = false;
        foreach (var sol1 in new Call().Apply(context, scope, new[] { args[1] }))
        {
            if (!sol1.Result)
            {
                yield return False();
                yield break;
            }

            yield return True(sol1.Substitutions);
            any = true;
        }

        _ = new Call().Apply(context, scope, new[] { args[2] }).FirstOrDefault();
        if (!any)
        {
            yield return False();
            yield break;
        }
    }
}
