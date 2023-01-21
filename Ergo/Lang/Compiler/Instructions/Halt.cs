namespace Ergo.Lang.Compiler.Instructions;

public sealed class Halt : ErgoInstruction
{
    public Halt() : base(WellKnown.OpCodes.Halt)
    {
    }

    public override void Execute(ErgoVM vm, ref ReadOnlySpan<byte> buf) { /* hardcoded */ }
}