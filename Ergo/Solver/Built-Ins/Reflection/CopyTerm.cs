namespace Ergo.Solver.BuiltIns;

public sealed class CopyTerm : SolverBuiltIn
{
    public CopyTerm()
        : base("", new("copy_term"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> args)
    {
        var copy = args[0].Instantiate(scope.InstantiationContext);
        if (!LanguageExtensions.Unify(args[1], copy).TryGetValue(out var subs))
        {
            yield return False();
            yield break;
        }

        yield return True(subs);
    }
}
