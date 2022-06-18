using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class NTuple : AbstractList
{
    public static readonly NTuple Empty = new(ImmutableArray<ITerm>.Empty);

    public NTuple(ImmutableArray<ITerm> head)
        : base(head)
    {
        CanonicalForm = Fold(Functor, EmptyElement.WithAbstractForm(Maybe.Some<IAbstractTerm>(Empty)), head)
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
    public static Maybe<NTuple> FromCanonical(ITerm term) => FromQuasiCanonical(term, Maybe.Some(true), Maybe.Some(true));
    public static Maybe<NTuple> FromQuasiCanonical(ITerm term, Maybe<bool> parenthesized = default, Maybe<bool> hasEmptyElement = default)
    {
        if (parenthesized.HasValue && term is Complex { IsParenthesized: var p } && p != parenthesized.GetOrThrow())
            return default;

        return Unfold(term, tail => true, WellKnown.Functors.Conjunction)
            .Reduce(some =>
            {
                var last = some.Last();
                if (hasEmptyElement.HasValue && last.Equals(Empty.CanonicalForm) != hasEmptyElement.GetOrThrow())
                    return default;
                return Maybe.Some(new NTuple(some));
            }, () => default);
    }
}
