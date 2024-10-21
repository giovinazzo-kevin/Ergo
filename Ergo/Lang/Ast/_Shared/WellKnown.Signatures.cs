namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Signatures
    {
        public static readonly Signature Unify = new(new Atom("unify"), 2, Modules.Prologue, default);
        public static readonly Signature True = new(Literals.True, 0, default, default);
        public static readonly Signature False = new(Literals.False, 0, default, default);
    }
}