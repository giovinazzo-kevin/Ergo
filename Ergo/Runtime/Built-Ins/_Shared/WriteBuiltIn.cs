using Ergo.Interpreter.Libraries;

namespace Ergo.Runtime.BuiltIns;

public abstract class WriteBuiltIn : BuiltIn
{
    public readonly bool Canonical;
    public readonly bool Quoted;
    public readonly bool Portrayed;

    public static readonly CompiledHook PORTRAY_1 = Hook.CompileJIT(WellKnown.Hooks.IO.Portray_1, throwIfNotDefined: false);

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

    public override ErgoVM.Op Compile()
    {
        return vm =>
        {
            var portrayVm = vm.ScopedInstance();
            foreach (var arg in vm.Args)
            {
                // https://www.swi-prolog.org/pldoc/man?predicate=portray/1
                if (Portrayed && arg is not Variable)
                {
                    PORTRAY_1.Call(portrayVm, arg);
                    if (portrayVm.NumSolutions > 0)
                        break; // Do nothing, the hook already took care of this term by calling write_raw.
                }
                var text = TransformText(Explain(arg));
                if (vm.Out.Encoding.IsSingleByte)
                {
                    text = text.Replace("⊤", "true");
                    text = text.Replace("⊥", "false");
                }
                vm.Out.Write(text);
                vm.Out.Flush();
            }
        };
    }
}
