using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class Set : AbstractList
{
    public static readonly Set Empty = new(ImmutableArray<ITerm>.Empty);

    public Set(ImmutableArray<ITerm> head)
        : base(head.OrderBy(x => x).Distinct())
    {
        CanonicalForm = Fold(Operator, EmptyElement, ImmutableArray.CreateRange(Contents))
            .Reduce<ITerm>(a => a, v => v, c => c);
    }
    public Set(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents)) { }
    public override Operator Operator => WellKnown.Operators.Set;
    public override Atom EmptyElement => WellKnown.Literals.EmptyBracyList;
    public override (string Open, string Close) Braces => ("{", "}");
    public override ITerm CanonicalForm { get; }

    protected override AbstractList Create(ImmutableArray<ITerm> head) => new Set(head);
    public static Maybe<Set> FromCanonical(ITerm term)
        => Unfold(term, WellKnown.Literals.EmptyBracyList, tail => true, WellKnown.Functors.Set).Select(some => new Set(some));
    public override Maybe<IAbstractTerm> FromCanonicalTerm(ITerm canonical) => FromCanonical(canonical).Select(x => (IAbstractTerm)x);
}
