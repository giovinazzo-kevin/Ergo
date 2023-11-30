using Ergo.Interpreter;
using Ergo.Lang.Compiler;

namespace Ergo.Shell;

public readonly struct ShellScope
{
    /// <summary>
    /// If true, the tracing debugger will engage on the next solution.
    /// </summary>
    public readonly bool TraceEnabled;
    public readonly VMFlags VMFlags;
    public readonly InterpreterScope InterpreterScope;
    public readonly KnowledgeBase KnowledgeBase;

    public ShellScope(InterpreterScope i, bool trace, KnowledgeBase kb, VMFlags vmFlags)
    {
        InterpreterScope = i;
        TraceEnabled = trace;
        KnowledgeBase = kb;
        VMFlags = vmFlags;
    }

    public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, TraceEnabled, KnowledgeBase, VMFlags);
    public ShellScope WithTrace(bool x) => new(InterpreterScope, x, KnowledgeBase, VMFlags);
    public ShellScope WithKnowledgeBase(KnowledgeBase kb) => new(InterpreterScope, TraceEnabled, kb, VMFlags);
    public ShellScope WithVMFlags(VMFlags flags) => new(InterpreterScope, TraceEnabled, KnowledgeBase, flags);

    public void Throw(string message) => InterpreterScope.ExceptionHandler.Throw(new ShellException(message));

}
