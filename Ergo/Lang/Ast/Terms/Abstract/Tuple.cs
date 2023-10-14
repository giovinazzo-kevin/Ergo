namespace Ergo.Lang.Ast;

public sealed class NTuple : AbstractList
{
    public static readonly NTuple Empty = new(ImmutableArray<ITerm>.Empty, default, false);

    public NTuple(ImmutableArray<ITerm> head = default, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : base(head, scope, parenthesized)
    {
        CanonicalForm = FoldNoEmptyTailParensSingle(Operator, EmptyElement, head).AsParenthesized(parenthesized);
    }
    public NTuple(IEnumerable<ITerm> contents, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : this(ImmutableArray.CreateRange(contents), scope, parenthesized) { }
    public override Operator Operator => WellKnown.Operators.Conjunction;
    public override Atom EmptyElement => WellKnown.Literals.EmptyCommaList;
    public override (string Open, string Close) Braces => ("(", ")");
    protected override ITerm CanonicalForm { get; }
    protected override AbstractList Create(ImmutableArray<ITerm> head, Maybe<ParserScope> scope, bool parenthesized) => new NTuple(head, scope, parenthesized);

    public static Maybe<NTuple> FromPseudoCanonical(ITerm term, Maybe<ParserScope> scope, Maybe<bool> parenthesized = default, Maybe<bool> hasEmptyElement = default)
    {
        if (parenthesized.TryGetValue(out var parens) && term is Complex { IsParenthesized: var p } && p != parens)
            return default;

        return Unfold(term, WellKnown.Literals.EmptyCommaList, tail => true, WellKnown.Functors.Conjunction)
            .Map(some =>
            {
                var last = some.Last();
                if (hasEmptyElement.TryGetValue(out var empty) && last.Equals(Empty.CanonicalForm) != empty)
                    return default;
                return Maybe.Some(new NTuple(some, scope, term.IsParenthesized));
            }, () => default);
    }
    public override string Explain(bool canonical)
    {
        if (canonical)
            return CanonicalForm.Explain(true);
        if (IsEmpty)
            return EmptyElement.Explain();
        // Special cases for tuples:
        // 1. 1-item tuples can only be created internally, e.g. when parsing queries.
        //    They don't need to be parenthesized, ever.
        if (Contents.Length == 1)
            return Contents.Single().Explain();
        // 2. They don't need to be parenthesized implicitly
        var joined = Contents.Join(t => t.Explain(), ", ");
        if (!CanonicalForm.IsParenthesized)
            return joined;
        return $"{Braces.Open}{joined}{Braces.Close}";
    }
}
