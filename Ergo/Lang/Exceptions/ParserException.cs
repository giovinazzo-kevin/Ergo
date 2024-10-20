using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class ParserException : ErgoException
{
    public readonly LegacyErgoParser.ErrorType ErrorType;

    public ParserException(LegacyErgoParser.ErrorType error, ErgoLexer.StreamState state, params object[] args)
        : base(ExceptionUtils.GetMessage(state, ExceptionUtils.GetParserError(error, args))) => ErrorType = error;
}
