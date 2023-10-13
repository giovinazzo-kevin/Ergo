namespace Ergo.Solver.BuiltIns;

public abstract class WriteBuiltIn : SolverBuiltIn
{
    public readonly bool Canonical;
    public readonly bool Quoted;
    public readonly bool Portrayed;

    protected WriteBuiltIn(string documentation, Atom functor, Maybe<int> arity, bool canon, bool quoted, bool portray)
        : base(documentation, functor, arity, WellKnown.Modules.IO)
    {
        Canonical = canon;
        Quoted = quoted;
        Portrayed = portray;
    }

    protected static ITerm AsQuoted(ITerm t, bool quoted)
    {
        if (quoted)
            return t;
        return t.Reduce<ITerm>(
            a => a.AsQuoted(false),
            v => v,
            c => c.WithFunctor(c.Functor.AsQuoted(false))
                  .WithArguments(c.Arguments
                    .Select(a => AsQuoted(a, false)).ToImmutableArray()),
            abs => abs
        );
    }

    protected virtual string TransformText(string text) => text;

    protected virtual string Explain(ITerm arg) => AsQuoted(arg, Quoted).Explain(Canonical);

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        foreach (var arg in args)
        {
            // https://www.swi-prolog.org/pldoc/man?predicate=portray/1
            if (Portrayed && arg is not Variable && WellKnown.Hooks.IO.Portray_1.IsDefined(context))
            {
                var any = false;
                foreach (var _ in WellKnown.Hooks.IO.Portray_1.Call(context, scope, ImmutableArray.Create(arg)))
                    any = true;
                if (any) goto ret; // Do nothing, the hook is responsible for writing the term at this point.
            }
            var text = TransformText(Explain(arg));
            if (context.Solver.Out.Encoding.IsSingleByte)
            {
                text = text.Replace("⊤", "true");
                text = text.Replace("⊥", "false");
            }
            context.Solver.Out.Write(text);
            context.Solver.Out.Flush();
        }
    ret:
        yield return new(WellKnown.Literals.True);
    }
}
