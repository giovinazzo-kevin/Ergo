using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Variant : BuiltIn
{
    public Variant()
        : base("", new("variant"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Goal Compile() => args => args[0].IsVariantOf(args[1]) ? ErgoVM.Ops.NoOp : ErgoVM.Ops.Fail;
}
