using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public sealed class SequenceType : BuiltIn
{
    public SequenceType()
        : base("", new("seq_type"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
    {
        var (type, seq) = (arguments[1], arguments[0]);
        if (seq is Variable)
        {
            solver.Throw(new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, seq.Explain()));
            yield return new(WellKnown.Literals.False);
            yield break;
        }

        if (seq.IsAbstract<List>(out _))
        {
            if (type.Unify(new Atom("list")).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.True, subs.ToArray());
                yield break;
            }
        }

        if (seq.IsAbstract<NTuple>(out _))
        {
            if (type.Unify(new Atom("comma_list")).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.True, subs.ToArray());
                yield break;
            }
        }

        if (seq.IsAbstract<Set>(out _))
        {
            if (type.Unify(new Atom("bracy_list")).TryGetValue(out var subs))
            {
                yield return new(WellKnown.Literals.True, subs.ToArray());
                yield break;
            }
        }

        yield return new(WellKnown.Literals.False);
    }
}
