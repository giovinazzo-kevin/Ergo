namespace Ergo.Lang.Ast;

public sealed class UntypedSequence : AbstractList
{
    public override Atom Functor { get; }
    public override Atom EmptyElement { get; }
    public override (string Open, string Close) Braces { get; }
    public override ITerm CanonicalForm { get; }

    public UntypedSequence(Atom functor, Atom emptyElem, (string Open, string Close) braces, ImmutableArray<ITerm> head)
        : base(head)
    {
        Functor = functor;
        EmptyElement = emptyElem;
        Braces = braces;
        CanonicalForm = Fold(Functor, EmptyElement, head).Reduce<ITerm>(a => a, v => v, c => c);
    }
    protected override AbstractList Create(ImmutableArray<ITerm> head) => throw new NotImplementedException();
}
