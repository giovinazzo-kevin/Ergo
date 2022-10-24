using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Ground : SolverBuiltIn
{
    public Ground()
        : base("", new("ground"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        yield return new(new Atom(arguments[0].IsGround));
    }
}
