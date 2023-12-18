namespace Ergo.Runtime.BuiltIns;

public sealed class Nonvar : BuiltIn
{
    public Nonvar()
        : base("", new("nonvar"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile()
    {
        return vm =>
        {
            if (!(vm.Arg(0) is not Variable))
                vm.Fail();
        };
    }
}
