namespace Ergo.Lang
{
    public partial class Lexer
    {
        public readonly ref struct Token
        {
            public readonly TokenType Type;
            public readonly object Value;

            public Token(TokenType type, object value)
            {
                Type = type;
                Value = value;
            }

            public static Token FromString(string value) => new Token(TokenType.String, value);
            public static Token FromNumber(decimal value) => new Token(TokenType.Number, value);
            public static Token FromKeyword(string value) => new Token(TokenType.Keyword, value);
            public static Token FromTerm(string value) => new Token(TokenType.Term, value);
            public static Token FromPunctuation(string value) => new Token(TokenType.Punctuation, value);
            public static Token FromOperator(string value) => new Token(TokenType.Operator, value);
            public static Token FromComment(string value) => new Token(TokenType.Comment, value);
        }
    }
}
