namespace Ergo.Solver.BuiltIns;

public sealed class Unifiable : SolverBuiltIn
{
    public Unifiable()
        : base("", new("unifiable"), Maybe<int>.Some(3), WellKnown.Modules.Prologue)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        if (arguments[0].Unify(arguments[1]).TryGetValue(out var subs))
        {
            var equations = subs.Select(s => (ITerm)new Complex(WellKnown.Operators.Unification.CanonicalFunctor, s.Lhs, s.Rhs)
                .AsOperator(WellKnown.Operators.Unification));
            List list = new(ImmutableArray.CreateRange(equations));
            if (new Substitution(arguments[2], list.CanonicalForm).Unify().TryGetValue(out subs))
            {
                yield return new(WellKnown.Literals.True, subs);
                yield break;
            }
        }

        yield return new(WellKnown.Literals.False);
    }
}
