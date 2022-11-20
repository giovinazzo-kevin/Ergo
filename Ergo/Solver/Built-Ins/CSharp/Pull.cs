namespace Ergo.Solver.BuiltIns;

public sealed class Pull : SolverBuiltIn
{
    public Pull()
        : base("", new("pull_data"), Maybe<int>.Some(1), WellKnown.Modules.CSharp)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var any = false;
        foreach (var item in context.Solver.GetDataSourceMatches(args[0]).ToEnumerable())
        {
            if (item.Rhs.Unify(args[0]).TryGetValue(out var subs))
            {
                any = true;
                yield return new(WellKnown.Literals.True, subs);
            }
        }

        if (!any)
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
