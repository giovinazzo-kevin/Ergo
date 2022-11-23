using Ergo.Interpreter;

namespace Ergo.Shell;

public readonly struct ShellScope
{
    /// <summary>
    /// If true, the tracing debugger will engage on the next solution.
    /// </summary>
    public readonly bool TraceEnabled;
    public readonly InterpreterScope InterpreterScope;
    public readonly KnowledgeBase KnowledgeBase;

    public ShellScope(InterpreterScope i, bool trace, KnowledgeBase kb)
    {
        InterpreterScope = i;
        TraceEnabled = trace;
        KnowledgeBase = kb;
    }

    public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, TraceEnabled, KnowledgeBase);
    public ShellScope WithTrace(bool x) => new(InterpreterScope, x, KnowledgeBase);
    public ShellScope WithKnowledgeBase(KnowledgeBase kb) => new(InterpreterScope, TraceEnabled, kb);

    public void Throw(string message) => InterpreterScope.ExceptionHandler.Throw(new ShellException(message));

}
