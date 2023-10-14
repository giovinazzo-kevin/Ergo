
namespace Ergo.Solver.BuiltIns;


public sealed class DictKeyValue : SolverBuiltIn
{
    public DictKeyValue()
        : base("", new($"dict_key_value"), Maybe<int>.Some(3), WellKnown.Modules.Dict)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        if (args[0] is Variable)
        {
            yield return ThrowFalse(scope, SolverError.TermNotSufficientlyInstantiated, args[0].Explain());
            yield break;
        }

        if (args[0] is Dict dict)
        {
            if (!dict.Dictionary.Keys.Any())
            {
                yield return False();
                yield break;
            }

            var anyKey = false;
            var anyValue = false;
            foreach (var key in dict.Dictionary.Keys)
            {
                var s1 = LanguageExtensions.Unify(args[1], key).TryGetValue(out var subs);
                if (s1)
                {
                    anyKey = true;
                    var s2 = LanguageExtensions.Unify(args[2], dict.Dictionary[key]).TryGetValue(out var vSubs);
                    if (s2)
                    {
                        anyValue = true;
                        yield return True(SubstitutionMap.MergeRef(vSubs, subs));
                    }
                    else
                    {
                        yield return False();
                        yield break;
                    }
                }
            }

            if (!anyKey)
            {
                yield return ThrowFalse(scope, SolverError.KeyNotFound, args[0].Explain(), args[1].Explain());
                yield break;
            }

            if (!anyValue)
            {
                yield return False();
            }

            yield break;
        }

        yield return False();
    }
}
