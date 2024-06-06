using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class RuntimeException(ErgoVM.ErrorType error, params object[] args) : ErgoException(ExceptionUtils.GetVMError(error, args))
{
    public readonly ErgoVM.ErrorType ErrorType = error;
}