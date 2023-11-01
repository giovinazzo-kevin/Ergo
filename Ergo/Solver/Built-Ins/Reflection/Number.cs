using PeterO.Numbers;

namespace Ergo.Solver.BuiltIns;

public sealed class Number : SolverBuiltIn
{
    public Number()
        : base("", new("number"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        yield return Bool(arguments[0] is Atom { Value: EDecimal _ });
    }
}
