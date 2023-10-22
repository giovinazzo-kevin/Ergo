namespace Ergo.Solver.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(2))
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        var eval = scope.InterpreterScope.ExceptionHandler.TryGet(() => new Atom(Evaluate(context.Solver, scope, arguments[1])));
        if (eval.TryGetValue(out var result) && LanguageExtensions.Unify(arguments[0], result).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }
        yield return False();
    }
}
