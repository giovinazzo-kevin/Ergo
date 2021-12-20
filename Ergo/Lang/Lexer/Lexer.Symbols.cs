using System.Linq;

namespace Ergo.Lang
{
    public partial class Lexer
    {
        public static readonly string[] KeywordSymbols = new string[] {
            "true", "false"
        };

        public static readonly string[] PunctuationSymbols = new string[] {
            "(", ")", "///", "[", "]", ",", "."
        };

        public readonly string[] OperatorSymbols;

    }
}
