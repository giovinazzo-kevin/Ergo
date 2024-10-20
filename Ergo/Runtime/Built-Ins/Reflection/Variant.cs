namespace Ergo.Runtime.BuiltIns;

public sealed class Variant : ErgoBuiltIn
{
    public Variant()
        : base("", new("variant"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override Op Compile()
    {
        return vm =>
        {
            if (!(vm.Arg(0).IsVariantOf(vm.Arg(1))))
                vm.Fail();
        };
    }
}
