namespace Ergo.Lang.Ast;

public sealed class UntypedSequence : AbstractList
{
    public override Atom Functor { get; }
    public override ITerm EmptyElement { get; }
    public override (string Open, string Close) Braces { get; }

    public UntypedSequence(Atom functor, ITerm emptyElem, (string Open, string Close) braces, ImmutableArray<ITerm> head)
        : base(head, default)
    {
        Functor = functor;
        EmptyElement = emptyElem;
        Braces = braces;
    }
}