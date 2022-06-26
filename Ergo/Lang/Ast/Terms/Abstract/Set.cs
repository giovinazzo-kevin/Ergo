using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class Set : AbstractList
{
    public static readonly Set Empty = new(ImmutableArray<ITerm>.Empty);

    public Set(ImmutableArray<ITerm> head)
        : base(head.OrderBy(x => x).Distinct())
    {
        CanonicalForm = FoldNoEmptyTail(Functor, EmptyElement.WithAbstractForm(Maybe.Some<IAbstractTerm>(Empty)), ImmutableArray.CreateRange(Contents))
            .Reduce<ITerm>(a => a, v => v, c => c)
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this));
    }
    public Set(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents)) { }
    public override Atom Functor => WellKnown.Functors.Set.First();
    public override Atom EmptyElement => WellKnown.Literals.EmptyBracyList;
    public override (string Open, string Close) Braces => ("{", "}");
    public override ITerm CanonicalForm { get; }

    protected override AbstractList Create(ImmutableArray<ITerm> head) => new Set(head);
    public static Maybe<Set> FromCanonical(ITerm term)
        => Unfold(term, tail => tail.Equals(Empty.CanonicalForm), WellKnown.Functors.Set).Map(some => new Set(some.SkipLast(1)));
}
