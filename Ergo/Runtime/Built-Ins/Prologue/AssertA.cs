namespace Ergo.Runtime.BuiltIns;

public sealed class AssertA : DynamicPredicateBuiltIn
{
    public AssertA()
        : base("", new("asserta"), 1)
    {
    }

    public override Op Compile() => vm =>
    {
        if (!Assert(vm, vm.Arg(0), z: false))
            vm.Fail();
    };
}
