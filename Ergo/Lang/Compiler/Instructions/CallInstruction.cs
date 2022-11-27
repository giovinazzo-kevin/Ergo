namespace Ergo.Lang.Compiler.Instructions;

public sealed class CallInstruction : ErgoInstruction
{
    public CallInstruction() : base(WellKnown.OpCodes.Call)
    {
    }

    public override void Deserialize(ErgoCompiler compiler, CompilerScope scope, ref ReadOnlySpan<byte> bytes)
    {

    }

    public override void Serialize(ErgoCompiler compiler, CompilerScope scope, ref Span<byte> bytes)
    {

    }
}