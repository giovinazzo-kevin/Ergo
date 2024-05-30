namespace Ergo.Runtime.BuiltIns;

public sealed class Explain : BuiltIn
{
    public Explain()
        : base("", "explain", Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var expl = new Atom(vm.Arg(0).AsQuoted(false).Explain(), quoted: false);
        vm.SetArg(0, vm.Arg(1));
        vm.SetArg(1, expl);
        ErgoVM.Goals.Unify2(vm);
    };
}
