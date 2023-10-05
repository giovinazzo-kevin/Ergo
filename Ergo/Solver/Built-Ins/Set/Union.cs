namespace Ergo.Solver.BuiltIns;

public sealed class Union : SolverBuiltIn
{
    public Union()
        : base("", new("union"), 3, WellKnown.Modules.Set)
    {

    }

    public override IEnumerable<Evaluation> Apply(SolverContext solver, SolverScope scope, ITerm[] args)
    {
        if (args[0].IsAbstract<Set>().TryGetValue(out var s1))
        {
            if (args[1].IsAbstract<Set>().TryGetValue(out var s2))
            {
                var s3 = new Set(s1.Contents.Union(s2.Contents));
                if (args[2].Unify(s3.CanonicalForm).TryGetValue(out var subs))
                {
                    yield return True(subs);
                    yield break;
                }
            }
            else if (args[2].IsAbstract<Set>().TryGetValue(out var s3))
            {
                s2 = new Set(s3.Contents.Except(s1.Contents));
                if (args[1].Unify(s2.CanonicalForm).TryGetValue(out var subs))
                {
                    yield return True(subs);
                    yield break;
                }
            }
        }
        else if (args[1].IsAbstract<Set>().TryGetValue(out var s2) && args[2].IsAbstract<Set>().TryGetValue(out var s3))
        {
            s1 = new Set(s3.Contents.Except(s2.Contents));
            if (args[0].Unify(s1.CanonicalForm).TryGetValue(out var subs))
            {
                yield return True(subs);
                yield break;
            }
        }
        yield return False();
        yield break;
    }
}
