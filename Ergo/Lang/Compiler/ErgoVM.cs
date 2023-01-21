using Ergo.Lang.Compiler.Instructions;

namespace Ergo.Lang.Compiler;

public partial class ErgoVM
{
    protected readonly byte[] Memory;
    protected readonly ErgoInstruction[] Instructions;

    public ErgoVM(byte[] knowledgeBase)
    {
        Memory = knowledgeBase;
        Instructions = new ErgoInstruction[byte.MaxValue];
    }

    public void DefineInstruction(ErgoInstruction instr)
    {
        if (Instructions[instr.OpCode] != null)
            throw new ArgumentException($"An instruction with opcode {instr.OpCode} was already registered");
        Instructions[instr.OpCode] = instr;
    }

    public ExecutionResult Run(ReadOnlySpan<byte> query)
    {
        while (Peek(query).TryGetValue(out var instr))
        {
            query = query[1..];
            // Hardcoded ops
            switch (instr.OpCode)
            {
                case WellKnown.OpCodes.Halt:
                    goto HALT;
            }
            try
            {
                instr.Execute(this, ref query);
            }
            catch (ErgoException)
            {
                return ExecutionResult.Exception;
            }
        }
    HALT:
        return ExecutionResult.Success;
    }

    public Maybe<ErgoInstruction> Peek(ReadOnlySpan<byte> span)
    {
        if (span.Length == 0)
            return default;
        if (Instructions[span[0]] is { } instr)
            return instr;
        return default;
    }
}
