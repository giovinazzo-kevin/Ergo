namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Literals
    {
        public static readonly Variable Discard = new("_");
        public static readonly Atom TopLevel = new("__toplevel");
        public static readonly Atom True = new(true);
        public static readonly Atom False = new(false);
        public static readonly Atom Cut = new("!");
        public static readonly Atom EmptyList = new("[]");
        public static readonly Atom EmptySet = new("{}");
        public static readonly Atom EmptyCommaList = new("()");

        public static readonly ITerm[] DefinedLiterals =
        [
            Discard, True, False, Cut, EmptyList, EmptySet, EmptyCommaList
        ];
    }
}