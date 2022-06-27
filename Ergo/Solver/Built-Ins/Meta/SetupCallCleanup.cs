namespace Ergo.Solver.BuiltIns;

public sealed class SetupCallCleanup : SolverBuiltIn
{
    public SetupCallCleanup()
        : base("", new("setup_call_cleanup"), 3, WellKnown.Modules.Meta)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        var once = await new Call().Apply(solver, scope, new[] { args[0] }).FirstOrDefaultAsync();
        if (once.Equals(default(Evaluation)) || once.Result.Equals(WellKnown.Literals.False))
        {
            yield return new(WellKnown.Literals.False);
            yield break;
        }

        var any = false;
        await foreach (var sol in new Call().Apply(solver, scope, new[] { args[1] }))
        {
            if (sol.Result.Equals(WellKnown.Literals.False))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, sol.Substitutions);
            any = true;
        }

        await new Call().Apply(solver, scope, new[] { args[2] }).FirstOrDefaultAsync();
        if (!any)
        {
            yield return new(WellKnown.Literals.False);
            yield break;
        }
    }
}
