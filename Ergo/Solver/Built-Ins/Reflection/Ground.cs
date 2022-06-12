using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Ground : BuiltIn
{
    public Ground()
        : base("", new("ground"), Maybe<int>.Some(1), Modules.Reflection)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        yield return new(new Atom(arguments[0].IsGround));
    }
}
