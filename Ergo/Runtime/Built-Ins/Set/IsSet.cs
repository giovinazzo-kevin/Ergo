namespace Ergo.Runtime.BuiltIns;

public sealed class IsSet : ErgoBuiltIn
{
    public IsSet()
        : base("", new("is_set"), 1, WellKnown.Modules.Set)
    {

    }

    public override Op Compile()
    {
        return vm =>
        {
            if (!(vm.Arg(0) is Set))
                vm.Fail();
        };
    }
}
