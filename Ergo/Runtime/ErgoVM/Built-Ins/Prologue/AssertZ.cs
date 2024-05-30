namespace Ergo.Runtime.BuiltIns;

public sealed class AssertZ : DynamicPredicateBuiltIn
{
    public AssertZ()
        : base("", "assertz", 1)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (!Assert(vm, vm.Arg(0), z: true))
            vm.Fail();
    };
}
