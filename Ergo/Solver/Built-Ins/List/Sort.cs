
namespace Ergo.Solver.BuiltIns;

public sealed class Sort : SolverBuiltIn
{
    public Sort()
        : base("", new("sort"), 2, WellKnown.Modules.List)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        if (args[0].IsAbstract<List>().TryGetValue(out var list))
        {
            var sorted = new List(list.Contents.OrderBy(x => x), default, list.Scope);
            if (args[1].Unify(sorted).TryGetValue(out var subs))
                yield return True(subs);
            else goto fail;
        }
        else if (args[1].IsAbstract<Set>().TryGetValue(out var set))
        {
            var lst = new List(set.Contents, default, set.Scope);
            if (args[0].Unify(lst).TryGetValue(out var subs))
                yield return True(subs);
            else goto fail;
        }
        else goto fail;
        yield break;
    fail:
        yield return False();
    }
}
