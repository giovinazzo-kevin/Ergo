
namespace Ergo.Solver.BuiltIns;

public sealed class ListSet : SolverBuiltIn
{
    public ListSet()
        : base("", new("list_set"), 2, WellKnown.Modules.List)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        if (args[0].IsAbstract<List>().TryGetValue(out var list))
        {
            var set = new Set(list.Contents);
            if (args[1].Unify(set.CanonicalForm).TryGetValue(out var subs))
                yield return True(subs);
            else goto fail;
        }
        else if (args[1].IsAbstract<Set>().TryGetValue(out var set))
        {
            var lst = new List(set.Contents);
            if (args[0].Unify(lst.CanonicalForm).TryGetValue(out var subs))
                yield return True(subs);
            else goto fail;
        }
        yield break;
    fail:
        yield return False();
    }
}
