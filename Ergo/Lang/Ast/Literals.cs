namespace Ergo.Lang
{
    public static class Literals
    {
        public static readonly Term Discard = new Variable("_");
        public static readonly Term True = new Atom(true);
        public static readonly Term False = new Atom(false);
        public static readonly Term Cut = new Atom("@cut");
        public static readonly Term EmptyList = List.EmptyLiteral;
        public static readonly Term EmptyCommaExpression = CommaExpression.EmptyLiteral;
    }
}