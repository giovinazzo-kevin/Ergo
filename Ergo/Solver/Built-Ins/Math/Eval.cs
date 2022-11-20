namespace Ergo.Solver.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(1))
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var eval = scope.InterpreterScope.ExceptionHandler.TryGet(() => new Evaluation(new Atom(Evaluate(context.Solver, scope, arguments[0]))));
        yield return eval.GetOr(False());
    }
}
