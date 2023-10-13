
namespace Ergo.Solver.BuiltIns;

public sealed class SequenceType : SolverBuiltIn
{
    public SequenceType()
        : base("", new("seq_type"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var (type, seq) = (arguments[1], arguments[0]);
        if (seq is Variable)
        {
            yield return ThrowFalse(scope, SolverError.TermNotSufficientlyInstantiated, seq.Explain());
            yield break;
        }

        if (seq is List)
        {
            if (type.Unify(new Atom("list")).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.True, subs);
                yield break;
            }
        }

        if (seq is NTuple)
        {
            if (type.Unify(new Atom("comma_list")).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.True, subs);
                yield break;
            }
        }

        if (seq is Set)
        {
            if (type.Unify(new Atom("bracy_list")).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.True, subs);
                yield break;
            }
        }

        yield return new(WellKnown.Literals.False);
    }
}
