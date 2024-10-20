using Ergo.Lang.Utils;
using Ergo.Modules;

namespace Ergo.Lang.Exceptions;

public class InterpreterException : ErgoException
{
    public InterpreterException(ErgoInterpreter.ErrorType error, params object[] args)
        : base(ExceptionUtils.GetInterpreterError(error, args))
    {
    }
}
