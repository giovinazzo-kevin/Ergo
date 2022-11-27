using Ergo.Lang.Compiler;
using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class CompilerException : ErgoException
{
    public readonly ErgoCompiler.ErrorType ErrorType;

    public CompilerException(ErgoCompiler.ErrorType error, params object[] args)
        : base(ExceptionUtils.GetCompilerError(error, args)) => ErrorType = error;
}
