using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class SetOf : SolutionAggregationBuiltIn
{
    public SetOf()
           : base("", new("setof"), Maybe.Some(3), Modules.Meta)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        var any = false;
        await foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(solver, scope, args))
        {
            var setTemplate = new List(ListTemplate.Contents
                .Distinct()
                .OrderBy(x => x));

            if (!ListVars.Root.Unify(ArgVars).TryGetValue(out var listSubs)
            || !args[2].Unify(setTemplate.Root).TryGetValue(out var instSubs))
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
