using Ergo.Lang.Compiler.Instructions;

namespace Ergo.Lang.Compiler;

public interface ICompilable
{
    IEnumerable<ErgoInstruction> Compile(ErgoCompiler compiler, CompilerScope scope);
}
