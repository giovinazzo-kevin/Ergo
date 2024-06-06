namespace Ergo.Lang.Ast;

public readonly struct DynamicPredicate(Signature sig, Predicate pred, bool assertz)
{
    public readonly Signature Signature = sig;
    public readonly Predicate Predicate = pred;
    public readonly bool AssertZ = assertz;
}
