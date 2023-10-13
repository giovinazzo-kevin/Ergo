namespace Ergo.Lang.Ast;

public sealed class Set : AbstractList
{
    public static readonly Set Empty = new(ImmutableArray<ITerm>.Empty, default);
    public Set(ImmutableArray<ITerm> head, Maybe<ParserScope> scope)
        : base(Sort(head), scope)
    {
        CanonicalForm = FoldNoEmptyTail(Operator, EmptyElement, ImmutableArray.CreateRange(Contents));
    }
    public Set(IEnumerable<ITerm> contents, Maybe<ParserScope> scope)
        : this(ImmutableArray.CreateRange(contents), scope) { }
    protected override AbstractList Create(ImmutableArray<ITerm> head, Maybe<ParserScope> scope)
        => new Set(head, scope);
    public override Operator Operator => WellKnown.Operators.Set;
    public override Atom EmptyElement => WellKnown.Literals.EmptyBracyList;
    public override (string Open, string Close) Braces => ("{", "}");
    protected override ITerm CanonicalForm { get; }
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

}
