namespace Ergo.Runtime.BuiltIns;

public sealed class Explain : ErgoBuiltIn
{
    public Explain()
        : base("", new("explain"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override Op Compile() => vm =>
    {
        var args = vm.Args;
        var expl = new Atom(args[0].AsQuoted(false).Explain(), false);
        vm.SetArg(0, args[1]);
        vm.SetArg(1, expl);
        ErgoVM.Goals.Unify2(vm);
    };
}
