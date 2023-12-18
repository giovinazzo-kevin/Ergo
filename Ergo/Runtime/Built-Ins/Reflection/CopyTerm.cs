namespace Ergo.Runtime.BuiltIns;

public sealed class CopyTerm : BuiltIn
{
    public CopyTerm()
        : base("", new("copy_term"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var copy = vm.Arg(0).Instantiate(vm.InstantiationContext);
        vm.SetArg(0, copy);
        ErgoVM.Goals.Unify2(vm);
    };
}
