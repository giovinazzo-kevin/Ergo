

namespace Ergo.Solver.BuiltIns;

public sealed class DictKeyValue : SolverBuiltIn
{
    public DictKeyValue()
        : base("", new($"dict_key_value"), Maybe<int>.Some(3), WellKnown.Modules.Dict)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (args[0] is Variable)
        {
            yield return scope.ThrowFalse(SolverError.TermNotSufficientlyInstantiated, args[0].Explain());
            yield break;
        }

        if (args[0].IsAbstract<Dict>(out var dict))
        {
            if (!dict.Dictionary.Keys.Any())
            {
                yield return new Evaluation(WellKnown.Literals.False);
                yield break;
            }

            var anyKey = false;
            var anyValue = false;
            foreach (var key in dict.Dictionary.Keys)
            {
                var s1 = args[1].Unify(key).TryGetValue(out var subs);
                if (s1)
                {
                    anyKey = true;
                    var s2 = args[2].Unify(dict.Dictionary[key]).TryGetValue(out var vSubs);
                    if (s2)
                    {
                        anyValue = true;
                        yield return new Evaluation(WellKnown.Literals.True, subs.Concat(vSubs).ToArray());
                    }
                    else
                    {
                        yield return new Evaluation(WellKnown.Literals.False);
                        yield break;
                    }
                }
            }

            if (!anyKey)
            {
                yield return scope.ThrowFalse(SolverError.KeyNotFound, args[1].Explain());
                yield break;
            }

            if (!anyValue)
            {
                yield return new Evaluation(WellKnown.Literals.False);
            }

            yield break;
        }

        yield return new Evaluation(WellKnown.Literals.False);
    }
}

