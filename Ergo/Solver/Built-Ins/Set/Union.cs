namespace Ergo.Solver.BuiltIns;

public sealed class Union : SolverBuiltIn
{
    public Union()
        : base("", new("union"), 3, WellKnown.Modules.Set)
    {

    }

    public override IEnumerable<Evaluation> Apply(SolverContext solver, SolverScope scope, ITerm[] args)
    {
        if (args[0] is Set s1)
        {
            if (args[1] is Set s2)
            {
                var s3 = new Set(s1.Contents.Union(s2.Contents), s1.Scope);
                if (LanguageExtensions.Unify(args[2], s3).TryGetValue(out var subs))
                {
                    yield return True(subs);
                    yield break;
                }
            }
            else if (args[2] is Set s3)
            {
                s2 = new Set(s3.Contents.Except(s1.Contents), s3.Scope);
                if (LanguageExtensions.Unify(args[1], s2).TryGetValue(out var subs))
                {
                    yield return True(subs);
                    yield break;
                }
            }
        }
        else if (args[1] is Set s2 && args[2] is Set s3)
        {
            s1 = new Set(s3.Contents.Except(s2.Contents), s3.Scope);
            if (LanguageExtensions.Unify(args[0], s1).TryGetValue(out var subs))
            {
                yield return True(subs);
                yield break;
            }
        }
        yield return False();
        yield break;
    }
}
