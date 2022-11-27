using Ergo.Interpreter;

namespace Ergo.Lang.Compiler;

public readonly struct CompilerScope
{
    public readonly InterpreterScope InterpreterScope;

    public CompilerScope(InterpreterScope interpreterScope)
    {
        InterpreterScope = interpreterScope;
    }
}
