namespace Ergo.Solver.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(2))
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        var eval = scope.InterpreterScope.ExceptionHandler.TryGet(() => new Atom(Evaluate(context.Solver, scope, arguments[0])));
        if (!eval.TryGetValue(out var atom))
        {
            yield return False();
            yield break;
        }
        if (arguments[1].Unify(atom).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }
        yield return False();
        yield break;
    }
}
