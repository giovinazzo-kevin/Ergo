
using System.Text.RegularExpressions;

namespace Ergo.Solver.BuiltIns;

public sealed class FormatString : SolverBuiltIn
{
    private readonly Regex PositionalParamRegex = new(@"(?<!{){(\d+)}(?!})");

    public FormatString()
        : base("", new("str_fmt"), Maybe<int>.Some(3), WellKnown.Modules.String)
    {
    }
    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var (format, args, result) = (arguments[0], arguments[1], arguments[2]);
        if (!format.Matches<string>(out var formatStr))
        {
            yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.String, format.Explain());
            yield break;
        }

        var items = args.IsAbstract<List>()
            .GetOr(new List(new[] { args }, default, args.Scope));

        if (result is not Atom resultStr)
        {
            if (result.IsGround)
            {
                yield return ThrowFalse(scope, SolverError.ExpectedTermOfTypeAt, WellKnown.Types.String, format.Explain());
                yield break;
            }
            else
            {
                resultStr = new(formatStr);
            }
        }

        var matchStart = string.Empty;
        var resultStrRaw = resultStr.AsQuoted(false).Explain(canonical: false);
        var formatStrRaw = formatStr;
        var matches = PositionalParamRegex.Matches(formatStrRaw).ToList();
        var varSubs = new SubstitutionMap();
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var argIndex = int.Parse(match.Groups[1].Value);
            var item = items.Contents.ElementAtOrDefault(argIndex);
            var ret = item?.Reduce<ITerm>(a => a.AsQuoted(false), v => v, c => c, a => a)?.Explain(canonical: false) ?? string.Empty;
            if (item is Variable v && result.IsGround)
            {
                // User is trying to match this variable from the result string
                // We want to capture it FROM the result string!
                var startIndex = i > 0 ? matches[i - 1].Index + matches[i - 1].Length : 0;
                var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : formatStrRaw.Length;
                var before = formatStrRaw.Substring(startIndex, match.Index - startIndex);
                var after = formatStrRaw.Substring(match.Index + match.Length, endIndex - (match.Index + match.Length));
                var capturePattern = $"{(startIndex == 0 ? "^" : "")}{matchStart}{before}(.+?){after}{(endIndex == formatStrRaw.Length ? "$" : "")}";
                if (Regex.Match(resultStrRaw, capturePattern) is { Success: true } capture)
                {
                    var atom = new Atom(capture.Groups[1].Value);
                    v.Unify(atom).TryGetValue(out var subs);
                    varSubs.AddRange(subs);
                    ret = atom.AsQuoted(false).Explain(canonical: false);
                }
                matchStart += before;
            }
            matchStart += ret;
            formatStr = formatStr.Replace(match.Value, ret);
        }

        if (result.IsGround && formatStr.Equals(resultStrRaw))
        {
            yield return new(WellKnown.Literals.True, varSubs);
        }
        else if (!result.IsGround)
        {
            yield return new(WellKnown.Literals.True, new Substitution(result, new Atom(formatStr)));
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
