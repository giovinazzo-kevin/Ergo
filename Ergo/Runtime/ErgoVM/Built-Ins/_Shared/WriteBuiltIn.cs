using Ergo.Interpreter.Libraries;
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public abstract class WriteBuiltIn(string documentation, Atom functor, Maybe<int> arity, bool canon, bool quoted, bool portray) : BuiltIn(documentation, functor, arity, WellKnown.Modules.IO)
{
    public readonly bool Canonical = canon;
    public readonly bool Quoted = quoted;
    public readonly bool Portrayed = portray;

    private readonly Hook PortrayHook = new(WellKnown.Hooks.IO.Portray_1);

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
        var portray = PortrayHook.Compile(throwIfNotDefined: false);
        return vm =>
        {
            var portrayVm = vm.ScopedInstance();
            portrayVm.Query = portray;

            foreach (var arg in vm.Args2[1..])
            {
                var tArg = vm.Memory.Dereference(arg);
                // https://www.swi-prolog.org/pldoc/man?predicate=portray/1
                if (Portrayed && arg is not VariableAddress)
                {
                    PortrayHook.SetArg(0, tArg);
                    portrayVm.Run();
                    if (portrayVm.NumSolutions > 0)
                        break; // Do nothing, the hook already took care of this term by calling write_raw.
                }
                var text = TransformText(Explain(tArg));
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
