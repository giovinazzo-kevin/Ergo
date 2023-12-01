using Ergo.Lang.Compiler;
using PeterO.Numbers;

namespace Ergo.Runtime.BuiltIns;

public sealed class Number : BuiltIn
{
    public Number()
        : base("", new("number"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }
    public override ErgoVM.Goal Compile() => args => args[0] is Atom { Value: EDecimal _ } ? ErgoVM.Ops.NoOp : ErgoVM.Ops.Fail;
}
