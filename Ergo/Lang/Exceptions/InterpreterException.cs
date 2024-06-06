using Ergo.Interpreter;
using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class InterpreterException(ErgoInterpreter.ErrorType error, InterpreterScope scope, params object[] args) : ErgoException(ExceptionUtils.GetInterpreterError(error, scope, args))
{
}
