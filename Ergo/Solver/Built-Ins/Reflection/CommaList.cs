
namespace Ergo.Solver.BuiltIns;

public sealed class CommaToList : SolverBuiltIn
{
    public CommaToList()
        : base("", new("comma_list"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        var (commaArg, listArg) = (arguments[0], arguments[1]);
        if (listArg is not Variable)
        {
            if (listArg is not List list)
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.List, listArg.Explain());
                yield break;
            }

            var comma = new NTuple(list.Contents, default);
            if (!LanguageExtensions.Unify(commaArg, comma).TryGetValue(out var subs))
            {
                yield return False();
                yield break;
            }

            yield return True(subs);
            yield break;
        }

        if (commaArg is not Variable)
        {
            if (commaArg is not NTuple comma)
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.CommaList, commaArg.Explain());
                yield break;
            }

            var list = new List(comma.Contents, default, default);
            if (!LanguageExtensions.Unify(listArg, list).TryGetValue(out var subs))
            {
                yield return False();
                yield break;
            }

            yield return True(subs);
            yield break;
        }

        yield return ThrowFalse(scope, SolverError.TermNotSufficientlyInstantiated, commaArg.Explain());
    }
}
