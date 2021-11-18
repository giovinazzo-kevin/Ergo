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

        public static readonly string[] OperatorSymbols = Operators.DefinedOperators
            .SelectMany(op => op.Synonyms
                .Select(s => (string)s.Value))
            .ToArray();

    }
}
