using Ergo.Modules;

namespace Ergo.Shell;

public readonly struct ShellScope
{
    /// <summary>
    /// If true, the tracing debugger will engage on the next solution.
    /// </summary>
    public readonly bool TraceEnabled;
    public readonly CompilerFlags CompilerFlags;
    public readonly InterpreterScope InterpreterScope;
    public readonly LegacyKnowledgeBase KnowledgeBase;

    public ShellScope(InterpreterScope i, bool trace, LegacyKnowledgeBase kb, CompilerFlags compilerFlags)
    {
        InterpreterScope = i;
        TraceEnabled = trace;
        KnowledgeBase = kb;
        CompilerFlags = compilerFlags;
    }

    public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, TraceEnabled, KnowledgeBase, CompilerFlags);
    public ShellScope WithTrace(bool x) => new(InterpreterScope, x, KnowledgeBase, CompilerFlags);
    public ShellScope WithKnowledgeBase(LegacyKnowledgeBase kb) => new(InterpreterScope, TraceEnabled, kb, CompilerFlags);
    public ShellScope WithCompilerFlags(CompilerFlags flags) => new(InterpreterScope, TraceEnabled, KnowledgeBase, flags);

    public void Throw(string message) => InterpreterScope.ExceptionHandler.Throw(new ShellException(message));

}
