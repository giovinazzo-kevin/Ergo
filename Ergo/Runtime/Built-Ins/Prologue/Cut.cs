namespace Ergo.Runtime.BuiltIns;

public sealed class Cut : ErgoBuiltIn
{
    public Cut()
        : base("", new("!"), Maybe<int>.Some(0), WellKnown.Modules.Prologue)
    {
    }

    public override Op Compile() => Ops.Cut;
}
