namespace Ergo.Solver.BuiltIns;

public sealed class CopyTerm : SolverBuiltIn
{
    public CopyTerm()
        : base("", new("copy_term"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var copy = args[0].Instantiate(scope.InterpreterScope.InstantaitionContext);
        if (!args[1].Unify(copy).TryGetValue(out var subs))
        {
            yield return False();
            yield break;
        }

        yield return True(subs);
    }
}
