namespace Ergo.Solver.BuiltIns;

public sealed class RetractAll : DynamicPredicateBuiltIn
{
    public RetractAll()
        : base("", new("retractall"), 1)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        if (Retract(context.Solver, scope, arguments[0], all: true)) yield return True();
        else yield return False();
    }
}
