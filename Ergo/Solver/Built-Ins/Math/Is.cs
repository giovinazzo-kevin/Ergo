namespace Ergo.Solver.BuiltIns;

public sealed class Is : MathBuiltIn
{
    public Is()
        : base("", new("eval_is"), Maybe<int>.Some(2))
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var eval = scope.InterpreterScope.ExceptionHandler.TryGet(() => new Atom(Evaluate(context.Solver, scope, arguments[1])));
        if (eval.TryGetValue(out var result) && arguments[0].Unify(result).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }

        yield return False();
    }
}
