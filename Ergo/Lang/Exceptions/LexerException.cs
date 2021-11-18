using Ergo.Lang.Utils;
using System;

namespace Ergo.Lang
{
    public class LexerException : Exception
    {
        public LexerException(Lexer.ErrorType error, Lexer.StreamState state, params object[] args)
            : base(ExceptionUtils.GetMessage(state, ExceptionUtils.GetLexerError(error, args)))
        {

        }
    }
}
