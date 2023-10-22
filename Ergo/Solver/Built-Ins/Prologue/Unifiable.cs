namespace Ergo.Solver.BuiltIns;

public sealed class Unifiable : SolverBuiltIn
{
    public Unifiable()
        : base("", new("unifiable"), Maybe<int>.Some(3), WellKnown.Modules.Prologue)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        if (LanguageExtensions.Unify(arguments[0], arguments[1]).TryGetValue(out var subs))
        {
            var equations = subs.Select(s => (ITerm)new Complex(WellKnown.Operators.Unification.CanonicalFunctor, s.Lhs, s.Rhs)
                .AsOperator(WellKnown.Operators.Unification));
            List list = new(ImmutableArray.CreateRange(equations), default, default);
            if (new Substitution(arguments[2], list).Unify().TryGetValue(out subs))
            {
                yield return True(subs);
                yield break;
            }
        }

        yield return False();
    }
}
