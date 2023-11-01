using PeterO.Numbers;

namespace Ergo.Solver.BuiltIns;

public sealed class NumberString : SolverBuiltIn
{
    public NumberString()
        : base("", new("number_string"), Maybe<int>.Some(2), WellKnown.Modules.Math)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext solver, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        var (str, num) = (arguments[1], arguments[0]);
        if (!str.IsGround && !num.IsGround)
        {
            yield return True();
            yield break;
        }
        else if (!str.IsGround && num.IsGround)
        {
            if (!str.Matches(out EDecimal d))
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Number, num);
                yield break;
            }
            if (LanguageExtensions.Unify(num, new Atom(d.ToString())).TryGetValue(out var subs))
            {
                yield return True(subs);
                yield break;
            }
        }
        else if (str.IsGround)
        {
            if (!str.Matches(out string s))
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.String, num);
                yield break;
            }
            EDecimal n = null;
            try
            {
                n = EDecimal.FromString(s);
            }
            catch { }
            if (n == null)
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Number, num);
                yield break;
            }
            if (LanguageExtensions.Unify(num, new Atom(n)).TryGetValue(out var subs))
            {
                yield return True(subs);
                yield break;
            }
        }
    }
}