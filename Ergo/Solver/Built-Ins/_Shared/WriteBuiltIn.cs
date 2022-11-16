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
                    .Select(a => AsQuoted(a, false)).ToImmutableArray())
        );
    }

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        // TODO: Move In/Out streams to the interpreter!!
        foreach (var arg in args)
        {
            // https://www.swi-prolog.org/pldoc/man?predicate=portray/1
            if (arg is not Variable && WellKnown.Hooks.IO.Portray_1.IsDefined(context))
            {
                var any = false;
                await foreach (var _ in WellKnown.Hooks.IO.Portray_1.Call(context, scope, ImmutableArray.Create(arg)))
                    any = true; // Do nothing, the hook is responsible for writing the term at this point.
                if (any) goto ret;
            }
            Console.Write(AsQuoted(arg, Quoted).Explain(Canonical));
        }
    ret:
        yield return new(WellKnown.Literals.True);
    }
}
