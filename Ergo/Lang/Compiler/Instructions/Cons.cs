namespace Ergo.Lang.Compiler.Instructions;

public sealed class Cons : ErgoInstruction
{
    public Cons() : base(WellKnown.OpCodes.Cons)
    {
    }

    public override void Execute(ErgoVM vm, ref ReadOnlySpan<byte> buf)
    {

    }
}