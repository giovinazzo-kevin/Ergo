namespace Ergo.Runtime.BuiltIns;

public sealed class AssertA : DynamicPredicateBuiltIn
{
    public AssertA()
        : base("", "asserta", 1)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (!Assert(vm, vm.Arg2(1), z: false))
            vm.Fail();
    };
}
