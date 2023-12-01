using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Cut : BuiltIn
{
    public Cut()
        : base("", new("!"), Maybe<int>.Some(0), WellKnown.Modules.Prologue)
    {
    }

    public override ErgoVM.Goal Compile() => args => ErgoVM.Ops.Cut;
}
