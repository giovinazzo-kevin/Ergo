using Ergo.Interpreter;
using Ergo.Solver;

namespace Ergo.Shell;

public readonly struct ShellScope
{
    /// <summary>
    /// If true, the tracing debugger will engage on the next solution.
    /// </summary>
    public readonly bool TraceEnabled;
    public readonly SolverFlags SolverFlags;
    public readonly InterpreterScope InterpreterScope;
    public readonly KnowledgeBase KnowledgeBase;

    public ShellScope(InterpreterScope i, bool trace, KnowledgeBase kb, SolverFlags flags)
    {
        InterpreterScope = i;
        TraceEnabled = trace;
        KnowledgeBase = kb;
        SolverFlags = flags;
    }

    public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, TraceEnabled, KnowledgeBase, SolverFlags);
    public ShellScope WithTrace(bool x) => new(InterpreterScope, x, KnowledgeBase, SolverFlags);
    public ShellScope WithKnowledgeBase(KnowledgeBase kb) => new(InterpreterScope, TraceEnabled, kb, SolverFlags);
    public ShellScope WithSolverFlags(SolverFlags flags) => new(InterpreterScope, TraceEnabled, KnowledgeBase, flags);

    public void Throw(string message) => InterpreterScope.ExceptionHandler.Throw(new ShellException(message));

}
