namespace Ergo.Runtime.BuiltIns;

public sealed class IsSet : BuiltIn
{
    public IsSet()
        : base("", "is_set", 1, WellKnown.Modules.Set)
    {

    }

    public override ErgoVM.Op Compile()
    {
        return vm =>
        {
            if (!(vm.Arg(0) is Set))
                vm.Fail();
        };
    }
}
