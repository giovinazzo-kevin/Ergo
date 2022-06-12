using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class Nonvar : BuiltIn
{
    public Nonvar()
        : base("", new("nonvar"), Maybe<int>.Some(1), Modules.Reflection)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        yield return new(new Atom(arguments[0] is not Variable));
    }
}
