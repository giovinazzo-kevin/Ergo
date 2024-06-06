using Ergo.Lang.Compiler;
using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class CompilerException(ErgoCompiler.ErrorType error, params object[] args) : ErgoException(ExceptionUtils.GetCompilerError(error, args))
{
    public readonly ErgoCompiler.ErrorType ErrorType = error;
}
