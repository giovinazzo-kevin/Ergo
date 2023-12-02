namespace Ergo.Runtime.BuiltIns;

public sealed class Explain : BuiltIn
{
    public Explain()
        : base("", new("explain"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        var expl = new Atom(args[0].AsQuoted(false).Explain(), false);
        vm.SetArg(0, args[1]);
        vm.SetArg(1, expl);
        ErgoVM.Goals.Unify2(vm);
    };
}
