

namespace Ergo.Solver.BuiltIns;

public sealed class Compare : SolverBuiltIn
{
    public Compare()
        : base("", new("compare"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        var cmp = arguments[1].CompareTo(arguments[2]);
        if (arguments[0].IsGround)
        {
            if (!arguments[0].Matches<int>(out var result))
            {
                scope.Throw(SolverError.ExpectedTermOfTypeAt, WellKnown.Types.Number, arguments[0].Explain());
                yield return False();
                yield break;
            }

            if (result.Equals(cmp))
            {
                yield return True();
            }
            else
            {
                yield return False();
            }

            yield break;
        }

        yield return True(new Substitution(arguments[0], new Atom(cmp)));
    }
}
