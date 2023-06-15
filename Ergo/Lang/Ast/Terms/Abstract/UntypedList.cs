using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class UntypedSequence : AbstractList
{
    public override Operator Operator { get; }
    public override Atom EmptyElement { get; }
    public override (string Open, string Close) Braces { get; }
    public override ITerm CanonicalForm { get; }

    public UntypedSequence(Operator op, Atom emptyElem, (string Open, string Close) braces, ImmutableArray<ITerm> head)
        : base(head)
    {
        Operator = op;
        EmptyElement = emptyElem;
        Braces = braces;
        CanonicalForm = Fold(Operator, EmptyElement, head).Reduce<ITerm>(a => a, v => v, c => c);
    }
    protected override AbstractList Create(ImmutableArray<ITerm> head) => throw new NotImplementedException();
    public override Maybe<IAbstractTerm> FromCanonicalTerm(ITerm canonical) => throw new NotImplementedException();
}