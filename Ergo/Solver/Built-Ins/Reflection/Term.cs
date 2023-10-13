
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
            if (termArg.IsAbstract<Dict>().TryGetValue(out var dict))
            {
                var tag = dict.Functor.Reduce<ITerm>(a => a, v => v);
                if (!functorArg.Unify(new Atom("dict")).TryGetValue(out var funSubs)
                || !args.Unify(new List(new[] { tag }.Append(new List(dict.KeyValuePairs, default, dict.Scope)), default, dict.Scope))
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
                if (!functorArg.Unify(complex.Functor).TryGetValue(out var funSubs)
                || !args.Unify(new List(complex.Arguments, default, complex.Scope)).TryGetValue(out var listSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }

                yield return new(WellKnown.Literals.True, SubstitutionMap.MergeRef(funSubs, listSubs));
                yield break;
            }

            if (termArg is Atom atom)
            {
                if (!functorArg.Unify(atom).TryGetValue(out var funSubs)
                || !args.Unify(WellKnown.Literals.EmptyList).TryGetValue(out var listSubs))
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

        if (!args.IsAbstract<List>().TryGetValue(out var argsList) || argsList.Contents.Length == 0)
        {
            if (args is not Variable && !args.Equals(WellKnown.Literals.EmptyList))
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.List, args.Explain());
                yield break;
            }

            if (!termArg.Unify(functor).TryGetValue(out var subs)
            || !args.Unify(WellKnown.Literals.EmptyList).TryGetValue(out var argsSubs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, SubstitutionMap.MergeRef(argsSubs, subs));
        }
        else
        {
            if (!termArg.Unify(new Complex(functor, argsList.Contents.ToArray())).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, subs);
        }
    }
}
