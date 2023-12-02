using PeterO.Numbers;

namespace Ergo.Runtime.BuiltIns;

public sealed class Number : BuiltIn
{
    public Number()
        : base("", new("number"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }
    public override ErgoVM.Op Compile()
    {
        return vm =>
        {
            if (!(vm.Arg(0) is Atom { Value: EDecimal _ }))
                vm.Fail();
        };
    }
}
