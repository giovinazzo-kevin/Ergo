namespace Ergo.Lang.Ast;

public readonly struct DynamicPredicate
{
    public readonly Signature Signature;
    public readonly Clause Predicate;
    public readonly bool AssertZ;

    public DynamicPredicate(Signature sig, Clause pred, bool assertz)
    {
        Signature = sig;
        Predicate = pred;
        AssertZ = assertz;
    }
}
