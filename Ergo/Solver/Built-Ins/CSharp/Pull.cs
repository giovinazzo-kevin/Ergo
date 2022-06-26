using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Pull : SolverBuiltIn
{
    public Pull()
        : base("", new("pull_data"), Maybe<int>.Some(1), WellKnown.Modules.CSharp)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        var any = false;
        await foreach (var item in solver.GetDataSourceMatches(args[0]))
        {
            if (item.Rhs.Unify(args[0]).TryGetValue(out var subs))
            {
                any = true;
                yield return new(WellKnown.Literals.True, subs.ToArray());
            }
        }

        if (!any)
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
