using Ergo.Lang.Compiler;

namespace Ergo.Lang.Ast;

public sealed class Set : AbstractList
{
    private static readonly SetCompiler SetCompiler = new();
    public override IAbstractTermCompiler Compiler => SetCompiler;

    public static readonly Set Empty = new([], default, false);
    public Set(ImmutableArray<ITerm> head, Maybe<ParserScope> scope, bool parenthesized)
        : base(Sort(head), scope, parenthesized)
    {
        CanonicalForm = Fold(Operator, EmptyElement, ImmutableArray.CreateRange(Contents));
    }
    public Set(IEnumerable<ITerm> contents, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : this(ImmutableArray.CreateRange(contents), scope, parenthesized) { }
    protected override AbstractList Create(ImmutableArray<ITerm> head, Maybe<ParserScope> scope = default, bool parenthesized = false)
        => new Set(head, scope, parenthesized);
    public override Operator Operator => WellKnown.Operators.Set;
    public override Atom EmptyElement => WellKnown.Literals.EmptySet;
    public override (string Open, string Close) Braces => ("{", "}");
    public override ITerm CanonicalForm { get; set; }
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
