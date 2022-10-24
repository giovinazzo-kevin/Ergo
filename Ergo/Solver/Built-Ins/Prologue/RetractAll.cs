namespace Ergo.Solver.BuiltIns;

public sealed class RetractAll : DynamicPredicateBuiltIn
{
    public RetractAll()
        : base("", new("retractall"), 1)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        if (Retract(context.Solver, scope, arguments[0], all: true)) yield return new(WellKnown.Literals.True);
        else yield return new(WellKnown.Literals.False);
    }
}
