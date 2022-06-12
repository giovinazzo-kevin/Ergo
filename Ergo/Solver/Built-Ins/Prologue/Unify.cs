using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Unify : BuiltIn
{
    public Unify()
        : base("", new("unify"), Maybe<int>.Some(2), Modules.Prologue)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        if (arguments[0].Unify(arguments[1]).TryGetValue(out var subs))
        {
            yield return new(WellKnown.Literals.True, subs.ToArray());
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
