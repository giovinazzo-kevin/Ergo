
namespace Ergo.Solver.BuiltIns;

public sealed class Sort : SolverBuiltIn
{
    public Sort()
        : base("", new("sort"), 2, WellKnown.Modules.List)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> args)
    {
        if (args[0] is List list)
        {
            var sorted = new List(list.Contents.OrderBy(x => x), default, list.Scope);
            if (LanguageExtensions.Unify(args[1], sorted).TryGetValue(out var subs))
                yield return True(subs);
            else goto fail;
        }
        else if (args[1] is Set set)
        {
            var lst = new List(set.Contents, default, set.Scope);
            if (LanguageExtensions.Unify(args[0], lst).TryGetValue(out var subs))
                yield return True(subs);
            else goto fail;
        }
        else goto fail;
        yield break;
    fail:
        yield return False();
    }
}
