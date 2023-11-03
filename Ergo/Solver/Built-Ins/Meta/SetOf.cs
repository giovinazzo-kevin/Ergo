namespace Ergo.Solver.BuiltIns;

public sealed class SetOf : SolutionAggregationBuiltIn
{
    public SetOf()
           : base("", new("setof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> args)
    {
        var any = false;
        foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(context, scope, args))
        {
            var argSet = new Set(ArgVars.Contents, ArgVars.Scope);
            var setVars = new Set(ListVars.Contents, ArgVars.Scope);
            var setTemplate = new Set(ListTemplate.Contents, ArgVars.Scope);

            if (!LanguageExtensions.Unify(setVars, argSet).TryGetValue(out var listSubs)
            || !LanguageExtensions.Unify(args[2], setTemplate).TryGetValue(out var instSubs))
            {
                yield return False();
                yield break;
            }

            yield return True(SubstitutionMap.MergeRef(listSubs, instSubs));
            any = true;
        }

        if (!any)
        {
            yield return False();
        }
    }
}
