namespace Ergo.Solver.BuiltIns;

public abstract class WriteBuiltIn : SolverBuiltIn
{
    public readonly bool Canonical;
    public readonly bool Quoted;

    protected WriteBuiltIn(string documentation, Atom functor, Maybe<int> arity, bool canon, bool quoted)
        : base(documentation, functor, arity, WellKnown.Modules.IO)
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
                    .Select(a => AsQuoted(a, false)).ToArray())
        );
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (args[0].IsAbstract<NTuple>(out var comma))
        {
            Console.Write(comma.Contents.Join(x =>
                AsQuoted(x, Quoted).Explain(canonical: Canonical), ""));
        }
        else
        {
            Console.Write(AsQuoted(args[0], Quoted).Explain(Canonical));
        }

        yield return new(WellKnown.Literals.True);
    }
}
