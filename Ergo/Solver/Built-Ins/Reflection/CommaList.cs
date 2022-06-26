

namespace Ergo.Solver.BuiltIns;

public sealed class CommaToList : SolverBuiltIn
{
    public CommaToList()
        : base("", new("comma_list"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var (commaArg, listArg) = (arguments[0], arguments[1]);
        if (listArg is not Variable)
        {
            if (!listArg.IsAbstract<List>(out var list))
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.List, listArg.Explain());
                yield break;
            }

            var comma = new NTuple(list.Contents);
            if (!commaArg.Unify(comma.CanonicalForm).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, subs.ToArray());
            yield break;
        }

        if (commaArg is not Variable)
        {
            if (!commaArg.IsAbstract<NTuple>(out var comma))
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.CommaList, commaArg.Explain());
                yield break;
            }

            var list = new List(comma.Contents);
            if (!listArg.Unify(list.CanonicalForm).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            yield return new(WellKnown.Literals.True, subs.ToArray());
            yield break;
        }

        yield return ThrowFalse(scope, SolverError.TermNotSufficientlyInstantiated, commaArg.Explain());
    }
}
