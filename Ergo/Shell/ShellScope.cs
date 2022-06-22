using Ergo.Interpreter;

namespace Ergo.Shell;

public readonly struct ShellScope
{
    public readonly InterpreterScope InterpreterScope;

    public readonly bool TraceEnabled;

    public ShellScope(InterpreterScope i, bool trace)
    {
        InterpreterScope = i;
        TraceEnabled = trace;
    }

    public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, TraceEnabled);
    public ShellScope WithTrace(bool x) => new(InterpreterScope, x);

    public void Throw(string message) => InterpreterScope.ExceptionHandler.Throw(new ShellException(message));

}
