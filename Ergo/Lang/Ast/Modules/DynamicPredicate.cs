namespace Ergo.Lang.Ast
{
    public readonly struct DynamicPredicate
    {
        public readonly Signature Signature;
        public readonly Predicate Predicate;
        public readonly bool AssertZ;

        public DynamicPredicate(Signature sig, Predicate pred, bool assertz)
        {
            Signature = sig;
            Predicate = pred;
            AssertZ = assertz;
        }
    }
}
