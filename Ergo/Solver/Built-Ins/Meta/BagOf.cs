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
            if (!LanguageExtensions.Unify(ListVars, ArgVars).TryGetValue(out var listSubs)
            || !LanguageExtensions.Unify(args[2], ListTemplate).TryGetValue(out var instSubs))
            {
                yield return False();
                yield break;
            }

            yield return True(SubstitutionMap.MergeRef(instSubs, listSubs));
            any = true;
        }

        if (!any)
        {
            yield return False();
        }
    }
}
