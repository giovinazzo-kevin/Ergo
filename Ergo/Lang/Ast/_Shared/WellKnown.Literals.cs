namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Literals
    {
        public static readonly Variable Discard = "_";
        public static readonly Atom TopLevel = "__toplevel";
        public static readonly Atom True = true;
        public static readonly Atom False = false;
        public static readonly Atom Cut = "!";
        public static readonly Atom EmptyList = "[]";
        public static readonly Atom EmptySet = "{}";
        public static readonly Atom EmptyCommaList = "()";

        public static readonly ITerm[] DefinedLiterals =
        [
            Discard, True, False, Cut, EmptyList, EmptySet, EmptyCommaList
        ];
    }
}