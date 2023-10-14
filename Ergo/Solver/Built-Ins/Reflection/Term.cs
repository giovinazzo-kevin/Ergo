
namespace Ergo.Solver.BuiltIns;

public sealed class Term : SolverBuiltIn
{
    public Term()
        : base("", new("term"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var (functorArg, args, termArg) = (arguments[0], arguments[1], arguments[2]);
        if (termArg is not Variable)
        {
            if (termArg is Dict dict)
            {
                var tag = dict.Functor.Reduce<ITerm>(a => a, v => v);
                if (!LanguageExtensions.Unify(functorArg, new Atom("dict")).TryGetValue(out var funSubs)
                || !LanguageExtensions.Unify(args, new List((new[] { tag }).Append(new List(dict.KeyValuePairs, default, dict.Scope)), default, dict.Scope))
                        .TryGetValue(out var listSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }

                yield return new(WellKnown.Literals.True, SubstitutionMap.MergeRef(funSubs, listSubs));
                yield break;
            }

            if (termArg is Complex complex)
            {
                if (!LanguageExtensions.Unify(functorArg, complex.Functor).TryGetValue(out var funSubs)
                || !LanguageExtensions.Unify(args, new List(complex.Arguments, default, complex.Scope)).TryGetValue(out var listSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }

                yield return new(WellKnown.Literals.True, SubstitutionMap.MergeRef(funSubs, listSubs));
                yield break;
            }

            if (termArg is Atom atom)
            {
                if (!LanguageExtensions.Unify(functorArg, atom).TryGetValue(out var funSubs)
                || !LanguageExtensions.Unify(args, WellKnown.Literals.EmptyList).TryGetValue(out var listSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }

                yield return new(WellKnown.Literals.True, SubstitutionMap.MergeRef(funSubs, listSubs));
                yield break;
            }
        }

        if (functorArg is Variable)
        {
            yield return ThrowFalse(scope, SolverError.TermNotSufficientlyInstantiated, functorArg.Explain());
            yield break;
        }

        if (functorArg is not Atom functor)
        {
            yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Atom, functorArg.Explain());
            yield break;
        }

        if (args is not List argsList || argsList.Contents.Length == 0)
        {
            if (args is not Variable && !args.Equals(WellKnown.Literals.EmptyList))
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.List, args.Explain());
                yield break;
            }

            if (!LanguageExtensions.Unify(termArg, functor).TryGetValue(out var subs)
            || !LanguageExtensions.Unify(args, WellKnown.Literals.EmptyList).TryGetValue(out var argsSubs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, SubstitutionMap.MergeRef(argsSubs, subs));
        }
        else
        {
            if (!LanguageExtensions.Unify(termArg, new Complex(functor, argsList.Contents.ToArray())).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, subs);
        }
    }
}
