using Ergo.Interpreter;
using Ergo.Lang.Exceptions;

namespace Ergo.Shell
{
    public readonly struct ShellScope
    {
        public readonly InterpreterScope InterpreterScope;
        public readonly ExceptionHandler ExceptionHandler;

        public ShellScope(InterpreterScope i, ExceptionHandler eh)
        {
            InterpreterScope = i;
            ExceptionHandler = eh;
        }

        public ShellScope WithInterpreterScope(InterpreterScope newScope) => new(newScope, ExceptionHandler);
        public ShellScope WithExceptionHandler(ExceptionHandler newHandler) => new(InterpreterScope, newHandler);

    }
}
