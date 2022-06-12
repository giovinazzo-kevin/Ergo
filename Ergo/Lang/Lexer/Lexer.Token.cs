namespace Ergo.Lang;

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

        public static Token FromString(string value) => new(TokenType.String, value);
        public static Token FromNumber(double value) => new(TokenType.Number, value);
        public static Token FromKeyword(string value) => new(TokenType.Keyword, value);
        public static Token FromITerm(string value) => new(TokenType.Term, value);
        public static Token FromPunctuation(string value) => new(TokenType.Punctuation, value);
        public static Token FromOperator(string value) => new(TokenType.Operator, value);
        public static Token FromComment(string value) => new(TokenType.Comment, value);
    }
}
