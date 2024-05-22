namespace Ergo.Runtime.BuiltIns;

public sealed class Cut : BuiltIn
{
    public Cut()
        : base("", new("!"), Maybe<int>.Some(0), WellKnown.Modules.Prologue)
    {
    }

    public override ErgoVM.Op Compile() => ErgoVM.Ops.Cut;
}
