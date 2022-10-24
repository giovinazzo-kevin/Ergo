namespace Ergo.Solver.BuiltIns;

public sealed class BagOf : SolutionAggregationBuiltIn
{
    public BagOf()
        : base("", new("bagof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var any = false;
        await foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(context.Solver, scope, args))
        {
            if (!ListVars.Unify(ArgVars).TryGetValue(out var listSubs)
            || !args[2].Unify(ListTemplate.CanonicalForm).TryGetValue(out var instSubs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, listSubs.Concat(instSubs).ToArray());
            any = true;
        }

        if (!any)
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
