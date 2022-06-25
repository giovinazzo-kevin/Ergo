using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class LexerException : ErgoException
{
    public readonly ErgoLexer.ErrorType ErrorType;

    public LexerException(ErgoLexer.ErrorType error, ErgoLexer.StreamState state, params object[] args)
        : base(ExceptionUtils.GetMessage(state, ExceptionUtils.GetLexerError(error, args))) => ErrorType = error;
}
