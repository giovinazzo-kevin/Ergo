namespace Ergo.Lang.Compiler.Instructions;

public abstract class ErgoInstruction
{
    public readonly byte OpCode;
    public ErgoInstruction(byte opCode) => OpCode = opCode;
    public abstract void Serialize(ErgoCompiler compiler, CompilerScope scope, ref Span<byte> bytes);
    public abstract void Deserialize(ErgoCompiler compiler, CompilerScope scope, ref ReadOnlySpan<byte> bytes);
    protected static void EnsureMemory(ref Span<byte> span, int numBytesNeeded)
    {
        if (span.Length < numBytesNeeded)
            throw new CompilerException(ErgoCompiler.ErrorType.NotEnoughMemoryToEmitNextInstruction, span.Length, numBytesNeeded);
    }
}
