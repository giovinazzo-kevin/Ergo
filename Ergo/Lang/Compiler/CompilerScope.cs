using Ergo.Interpreter;

namespace Ergo.Lang.Compiler;

public readonly struct CompilerScope
{
    public readonly InterpreterScope InterpreterScope;

    public readonly ImmutableArray<Atom> Constants;

    public CompilerScope(InterpreterScope interpreterScope, ImmutableArray<Atom> constants)
    {
        InterpreterScope = interpreterScope;
        Constants = constants;
    }

    public CompilerScope WithConstant(Atom c) => new(InterpreterScope, Constants.Add(c));
}
