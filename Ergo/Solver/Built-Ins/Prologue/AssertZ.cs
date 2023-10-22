namespace Ergo.Solver.BuiltIns;

public sealed class AssertZ : DynamicPredicateBuiltIn
{
    public AssertZ()
        : base("", new("assertz"), 1)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        if (Assert(context.Solver, scope, arguments[0], z: true))
        {
            yield return True();
        }
        else
        {
            yield return False();
        }
    }
}
