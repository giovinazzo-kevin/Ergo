namespace Ergo.Lang.Compiler.Instructions;

public abstract class ErgoInstruction
{
    public readonly byte OpCode;
    public ErgoInstruction(byte opCode) => OpCode = opCode;
    public abstract void Execute(ErgoVM vm, ref ReadOnlySpan<byte> buf);
    protected static void EnsureMemory(ref Span<byte> span, int numBytesNeeded)
    {
        if (span.Length < numBytesNeeded)
            throw new CompilerException(ErgoCompiler.ErrorType.NotEnoughMemoryToEmitNextInstruction, span.Length, numBytesNeeded);
    }
}
