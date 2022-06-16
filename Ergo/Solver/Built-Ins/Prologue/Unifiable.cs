using Ergo.Interpreter;
using System.Collections.Immutable;

namespace Ergo.Solver.BuiltIns;

public sealed class Unifiable : BuiltIn
{
    public Unifiable()
        : base("", new("unifiable"), Maybe<int>.Some(3), Modules.Prologue)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        if (arguments[0].Unify(arguments[1]).TryGetValue(out var subs))
        {
            var equations = subs.Select(s => (ITerm)new Complex(WellKnown.Functors.Unification.First(), s.Lhs, s.Rhs)
                .AsOperator(OperatorAffix.Infix));
            List list = new(ImmutableArray.CreateRange(equations));
            if (new Substitution(arguments[2], list.CanonicalForm).Unify().TryGetValue(out subs))
            {
                yield return new(WellKnown.Literals.True, subs.ToArray());
                yield break;
            }
        }

        yield return new(WellKnown.Literals.False);
    }
}
