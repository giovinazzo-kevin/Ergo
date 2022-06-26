using Ergo.Interpreter;

namespace Ergo.Shell;

public readonly struct ShellScope
{
    /// <summary>
    /// If true, the tracing debugger will engage on the next solution.
    /// </summary>
    public readonly bool TraceEnabled;
    public readonly InterpreterScope InterpreterScope;

    public ShellScope(InterpreterScope i, bool trace)
    {
        InterpreterScope = i;
        TraceEnabled = trace;
    }

    public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, TraceEnabled);
    public ShellScope WithTrace(bool x) => new(InterpreterScope, x);

    public void Throw(string message) => InterpreterScope.ExceptionHandler.Throw(new ShellException(message));

}
