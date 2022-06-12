using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public abstract class WriteBuiltIn : BuiltIn
{
    public readonly bool Canonical;
    public readonly bool Quoted;

    protected WriteBuiltIn(string documentation, Atom functor, Maybe<int> arity, bool canon, bool quoted)
        : base(documentation, functor, arity, Modules.IO)
    {
        Canonical = canon;
        Quoted = quoted;
    }

    static ITerm AsQuoted(ITerm t, bool quoted)
    {
        if (quoted)
            return t;
        return t.Reduce<ITerm>(
            a => a.AsQuoted(false),
            v => v,
            c => c.WithFunctor(c.Functor.AsQuoted(false))
                  .WithArguments(c.Arguments
                    .Select(a => AsQuoted(a, false)).ToArray()),
            d => new Dict(d.Functor.Map(a => a.AsQuoted(false), v => v), d.Dictionary
                .Select(kvp => new KeyValuePair<Atom, ITerm>(kvp.Key.AsQuoted(false), AsQuoted(kvp.Value, false))))
        );
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (CommaSequence.TryUnfold(args[0], out var comma))
        {
            Console.Write(String.Join(String.Empty, comma.Contents.Select(x =>
                AsQuoted(x, Quoted).Explain(canonical: Canonical))));
        }
        else
        {
            Console.Write(AsQuoted(args[0], Quoted).Explain(Canonical));
        }
        yield return new(WellKnown.Literals.True);
    }
}
