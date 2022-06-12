using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class ParserException : Exception
{
    public readonly Parser.ErrorType Kind;

    public ParserException(Parser.ErrorType error, Lexer.StreamState state, params object[] args)
        : base(ExceptionUtils.GetMessage(state, ExceptionUtils.GetParserError(error, args)))
    {
        Kind = error;
    }
}
