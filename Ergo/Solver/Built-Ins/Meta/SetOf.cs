namespace Ergo.Solver.BuiltIns;

public sealed class SetOf : SolutionAggregationBuiltIn
{
    public SetOf()
           : base("", new("setof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var any = false;
        await foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(context.Solver, scope, args))
        {
            var argSet = new Set(ArgVars.Contents);
            var setVars = new Set(ListVars.Contents);
            var setTemplate = new Set(ListTemplate.Contents);

            if (!setVars.Unify(argSet).TryGetValue(out var listSubs)
            || !args[2].Unify(setTemplate.CanonicalForm).TryGetValue(out var instSubs))
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
