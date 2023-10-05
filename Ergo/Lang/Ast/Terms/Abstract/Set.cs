using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class Set : AbstractList
{
    public static readonly Set Empty = new(ImmutableArray<ITerm>.Empty);

    static IEnumerable<ITerm> Sort(IEnumerable<ITerm> terms)
    {
        var sorted = terms
            .Where(t => t is not Variable)
            .OrderBy(x => x)
            .Distinct()
            .ToList();
        int i = 0;
        foreach (var item in terms)
        {
            if (item is Variable)
                sorted.Insert(i, item);
            i++;
        }
        return sorted;
    }

    public Set(ImmutableArray<ITerm> head)
        : base(Sort(head))
    {
        CanonicalForm = FoldNoEmptyTail(Operator, EmptyElement, ImmutableArray.CreateRange(Contents))
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
