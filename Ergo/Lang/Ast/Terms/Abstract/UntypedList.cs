namespace Ergo.Lang.Ast;

public sealed class UntypedSequence : AbstractList
{
    public override Operator Operator { get; }
    public override Atom EmptyElement { get; }
    public override (string Open, string Close) Braces { get; }
    protected override ITerm CanonicalForm { get; }

    public UntypedSequence(Operator op, Atom emptyElem, (string Open, string Close) braces, ImmutableArray<ITerm> head, Maybe<ParserScope> scope)
        : base(head, scope)
    {
        Operator = op;
        EmptyElement = emptyElem;
        Braces = braces;
        CanonicalForm = Fold(Operator, EmptyElement, head);
    }
    protected override AbstractList Create(ImmutableArray<ITerm> head, Maybe<ParserScope> scope) => throw new NotImplementedException();
}