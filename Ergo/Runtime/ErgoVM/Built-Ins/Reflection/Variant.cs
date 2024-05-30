namespace Ergo.Runtime.BuiltIns;

public sealed class Variant : BuiltIn
{
    public Variant()
        : base("", "variant", Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile()
    {
        return vm =>
        {
            if (!(vm.Arg(0).IsVariantOf(vm.Arg(1))))
                vm.Fail();
        };
    }
}
