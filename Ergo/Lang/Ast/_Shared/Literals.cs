namespace Ergo.Lang.Ast
{
    public static class Literals
    {
        public static readonly ITerm Discard = new Variable("_");
        public static readonly ITerm True = new Atom(true);
        public static readonly ITerm False = new Atom(false);
        public static readonly ITerm Cut = new Atom("@cut");
        public static readonly ITerm EmptyList = List.EmptyLiteral;
        public static readonly ITerm EmptyCommaExpression = CommaSequence.EmptyLiteral;
    }
}