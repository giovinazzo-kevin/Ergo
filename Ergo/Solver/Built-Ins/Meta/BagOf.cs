namespace Ergo.Solver.BuiltIns;

public sealed class BagOf : SolutionAggregationBuiltIn
{
    public BagOf()
        : base("", new("bagof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var any = false;
        foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(context.Solver, scope, args))
        {
            if (!ListVars.Unify(ArgVars).TryGetValue(out var listSubs)
            || !args[2].Unify(ListTemplate).TryGetValue(out var instSubs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, SubstitutionMap.MergeRef(instSubs, listSubs));
            any = true;
        }

        if (!any)
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
