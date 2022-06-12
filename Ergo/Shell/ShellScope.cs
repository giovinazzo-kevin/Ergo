using Ergo.Interpreter;
using Ergo.Lang.Exceptions;

namespace Ergo.Shell;

public readonly struct ShellScope
{
    public readonly InterpreterScope InterpreterScope;
    public readonly ExceptionHandler ExceptionHandler;

    public readonly bool TraceEnabled;
    public readonly bool ExceptionThrowingEnabled;

    public ShellScope(InterpreterScope i, ExceptionHandler eh, bool trace, bool ex)
    {
        InterpreterScope = i;
        ExceptionHandler = eh;
        TraceEnabled = trace;
        ExceptionThrowingEnabled = ex;
    }

    public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, ExceptionHandler, TraceEnabled, ExceptionThrowingEnabled);
    public ShellScope WithExceptionHandler(ExceptionHandler newHandler) => new(InterpreterScope, newHandler, TraceEnabled, ExceptionThrowingEnabled);
    public ShellScope WithTrace(bool x) => new(InterpreterScope, ExceptionHandler, x, ExceptionThrowingEnabled);
    public ShellScope WithExceptionThrowing(bool x) => new(InterpreterScope, ExceptionHandler, TraceEnabled, x);

}
