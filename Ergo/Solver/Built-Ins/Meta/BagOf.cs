using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class BagOf : SolutionAggregationBuiltIn
{
    public BagOf()
        : base("", new("bagof"), Maybe.Some(3), WellKnown.Modules.Meta)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        var any = false;
        await foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(solver, scope, args))
        {
            if (!ListVars.CanonicalForm.Unify(ArgVars).TryGetValue(out var listSubs)
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
