namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Lexemes
    {
        public static readonly char[] IdentifierPunctuation = [
            '@', '_', '(', ')', '[', ']', '{', '}'
        ];
        public static readonly char[] QuotablePunctuation = [
            ',', '.',  '(', ')', '[', ']', '{', '}'
        ];
    }

}
