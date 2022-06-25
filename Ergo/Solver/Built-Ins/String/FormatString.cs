
using System.Text.RegularExpressions;

namespace Ergo.Solver.BuiltIns;

public sealed class FormatString : SolverBuiltIn
{
    private readonly Regex PositionalParamRegex = new(@"(?<!{){(\d+)}(?!})");

    public FormatString()
        : base("", new("str_fmt"), Maybe<int>.Some(3), WellKnown.Modules.String)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var (format, args, result) = (arguments[0], arguments[1], arguments[2]);
        if (!format.Matches<string>(out var formatStr))
        {
            yield return scope.ThrowFalse(SolverError.ExpectedTermOfTypeAt, WellKnown.Types.String, format.Explain());
            yield break;
        }

        if (!args.IsAbstract<List>(out var items))
        {
            items = new List(new[] { args });
        }

        if (result is not Atom resultStr)
        {
            if (result.IsGround)
            {
                yield return scope.ThrowFalse(SolverError.ExpectedTermOfTypeAt, WellKnown.Types.String, format.Explain());
                yield break;
            }
            else
            {
                resultStr = new(formatStr);
            }
        }

        var testStr = PositionalParamRegex.Replace(formatStr, match =>
        {
            var argIndex = int.Parse(match.Groups[1].Value);
            var item = items.Contents.ElementAtOrDefault(argIndex);
            return item?.Reduce<ITerm>(a => a.AsQuoted(false), v => v, c => c)?.Explain(canonical: false) ?? string.Empty;
        });

        if (result.IsGround && testStr.Equals(resultStr.AsQuoted(false).Explain(canonical: false)))
        {
            yield return new(WellKnown.Literals.True);
        }
        else if (!result.IsGround)
        {
            yield return new(WellKnown.Literals.True, new Substitution(result, new Atom(testStr)));
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
