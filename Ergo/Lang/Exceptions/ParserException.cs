using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class ParserException : ErgoException
{
    public readonly ErgoParser.ErrorType ErrorType;

    public ParserException(ErgoParser.ErrorType error, Lexer.StreamState state, params object[] args)
        : base(ExceptionUtils.GetMessage(state, ExceptionUtils.GetParserError(error, args))) => ErrorType = error;
}
