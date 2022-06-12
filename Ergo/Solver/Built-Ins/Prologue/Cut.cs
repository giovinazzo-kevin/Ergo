using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Cut : BuiltIn
{
    public Cut()
        : base("", new("!"), Maybe<int>.Some(0), Modules.Prologue)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        yield return new(WellKnown.Literals.True);
    }
}
