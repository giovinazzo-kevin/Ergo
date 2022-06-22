using Ergo.Interpreter;
using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class InterpreterException : ErgoException
{
    public InterpreterException(InterpreterError error, InterpreterScope scope, params object[] args)
        : base(ExceptionUtils.GetInterpreterError(error, scope, args))
    {
    }
}
