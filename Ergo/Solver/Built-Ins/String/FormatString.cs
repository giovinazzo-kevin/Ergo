using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Solver.BuiltIns
{
    public sealed class FormatString : BuiltIn
    {
        private readonly Regex PositionalParamRegex = new(@"(?<!{){(\d+)}(?!})");

        public FormatString()
            : base("", new("str_fmt"), Maybe<int>.Some(3), Modules.String)
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var (format, args, result) = (arguments[0], arguments[1], arguments[2]);
            if (!format.Matches<string>(out var formatStr))
            {
                solver.Throw(new SolverException(SolverError.ExpectedTermOfTypeAt, scope, Types.String, format.Explain()));
                yield break;
            }
            if (!List.TryUnfold(args, out var items))
            {
                items = new List(args);
            }
            if (!result.Matches<string>(out var resultStr))
            {
                if(result.IsGround)
                {
                    solver.Throw(new SolverException(SolverError.ExpectedTermOfTypeAt, scope, Types.String, format.Explain()));
                    yield break;
                }
                else
                {
                    resultStr = formatStr;
                }
            }
            var testStr = PositionalParamRegex.Replace(resultStr, match =>
            {
                var argIndex = int.Parse(match.Groups[1].Value);
                var item = items.Contents.ElementAtOrDefault(argIndex);
                return item?.Explain() ?? string.Empty;
            });

            if(result.IsGround && testStr.Equals(resultStr))
            {
                yield return new(WellKnown.Literals.True);
            }
            else if(!result.IsGround)
            {
                yield return new(WellKnown.Literals.True, new Substitution(result, new Atom(testStr)));
            }
            else
            {
                yield return new(WellKnown.Literals.False);
            }
        }
    }
}
