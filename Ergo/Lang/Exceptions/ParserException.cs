using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class ParserException(ErgoParser.ErrorType error, ErgoLexer.StreamState state, params object[] args) : ErgoException(ExceptionUtils.GetMessage(state, ExceptionUtils.GetParserError(error, args)))
{
    public readonly ErgoParser.ErrorType ErrorType = error;
}
