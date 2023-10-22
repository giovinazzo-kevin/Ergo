namespace Ergo.Solver.BuiltIns;

public sealed class AssertA : DynamicPredicateBuiltIn
{
    public AssertA()
        : base("", new("asserta"), 1)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        if (Assert(context.Solver, scope, arguments[0], z: false))
        {
            yield return True();
        }
        else
        {
            yield return False();
        }
    }
}
