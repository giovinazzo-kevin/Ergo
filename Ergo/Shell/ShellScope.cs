using Ergo.Interpreter;

namespace Ergo.Shell;

public readonly struct ShellScope(InterpreterScope i, bool trace, KnowledgeBase kb, CompilerFlags compilerFlags)
{
    /// <summary>
    /// If true, the tracing debugger will engage on the next solution.
    /// </summary>
    public readonly bool TraceEnabled = trace;
    public readonly CompilerFlags CompilerFlags = compilerFlags;
    public readonly InterpreterScope InterpreterScope = i;
    public readonly KnowledgeBase KnowledgeBase = kb;

    public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, TraceEnabled, KnowledgeBase, CompilerFlags);
    public ShellScope WithTrace(bool x) => new(InterpreterScope, x, KnowledgeBase, CompilerFlags);
    public ShellScope WithKnowledgeBase(KnowledgeBase kb) => new(InterpreterScope, TraceEnabled, kb, CompilerFlags);
    public ShellScope WithCompilerFlags(CompilerFlags flags) => new(InterpreterScope, TraceEnabled, KnowledgeBase, flags);

    public void Throw(string message) => InterpreterScope.ExceptionHandler.Throw(new ShellException(message));

}
