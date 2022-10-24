namespace Ergo.Solver.BuiltIns;

public sealed class Retract : DynamicPredicateBuiltIn
{
    public Retract()
        : base("", new("retract"), 1)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var any = false;
        while (Retract(context.Solver, scope, arguments[0], all: false))
        {
            yield return new(WellKnown.Literals.True);
            any = true;
        }

        if (!any)
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
