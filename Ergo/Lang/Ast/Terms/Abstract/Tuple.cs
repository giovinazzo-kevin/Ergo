using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class NTuple : AbstractList
{
    public static readonly NTuple Empty = new(ImmutableArray<ITerm>.Empty);

    public NTuple(ImmutableArray<ITerm> head)
        : base(head)
    {
        CanonicalForm = FoldNoEmptyTail(Functor, EmptyElement.WithAbstractForm(Maybe.Some<IAbstractTerm>(Empty)), head)
            .Reduce<ITerm>(a => a, v => v, c => c)
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this));
    }
    public NTuple(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents)) { }
    public override Atom Functor => WellKnown.Functors.Tuple.First();
    public override Atom EmptyElement => WellKnown.Literals.EmptyCommaList;
    public override (string Open, string Close) Braces => ("(", ")");
    public override ITerm CanonicalForm { get; }
    protected override AbstractList Create(ImmutableArray<ITerm> head) => new NTuple(head);
    public static Maybe<NTuple> FromCanonical(ITerm term) => FromPseudoCanonical(term, true, true);
    public static Maybe<NTuple> FromPseudoCanonical(ITerm term, Maybe<bool> parenthesized = default, Maybe<bool> hasEmptyElement = default)
    {
        if (parenthesized.TryGetValue(out var parens) && term is Complex { IsParenthesized: var p } && p != parens)
            return default;

        return Unfold(term, tail => true, WellKnown.Functors.Conjunction)
            .Map(some =>
            {
                var last = some.Last();
                if (hasEmptyElement.TryGetValue(out var empty) && last.Equals(Empty.CanonicalForm) != empty)
                    return default;
                return Maybe.Some(new NTuple(some));
            }, () => default);
    }
}
