namespace Ergo.Solver.BuiltIns;

public sealed class TermType : SolverBuiltIn
{
    public TermType()
        : base("", new("term_type"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var type = arguments[0] switch
        {
            Atom => new Atom("atom"),
            Variable => new Atom("variable"),
            Complex => new Atom("complex"),
            _ => throw new NotSupportedException()
        };

        if (!arguments[1].IsGround)
        {
            yield return new(WellKnown.Literals.True, new Substitution(arguments[1], type));
        }
        else if (arguments[1].Unify(type).TryGetValue(out var subs))
        {
            yield return new(WellKnown.Literals.True, subs);
        }
        else
        {
            yield return new(WellKnown.Literals.False);
        }
    }
}
