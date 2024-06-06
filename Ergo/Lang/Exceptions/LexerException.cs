using Ergo.Lang.Utils;

namespace Ergo.Lang.Exceptions;

public class LexerException(ErgoLexer.ErrorType error, ErgoLexer.StreamState state, params object[] args) : ErgoException(ExceptionUtils.GetMessage(state, ExceptionUtils.GetLexerError(error, args)))
{
    public readonly ErgoLexer.ErrorType ErrorType = error;
}
