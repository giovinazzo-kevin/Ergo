using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class List : AbstractList
{
    public static readonly List Empty = new(ImmutableArray<ITerm>.Empty);
    public readonly ITerm Tail;
    public List(ImmutableArray<ITerm> contents, Maybe<ITerm> tail = default)
        : base(contents)
    {
        Tail = tail.GetOr(EmptyElement);
        CanonicalForm = Fold(Operator, Tail, contents)
            .Reduce<ITerm>(a => a, v => v, c => c);
    }
    public List(IEnumerable<ITerm> contents, Maybe<ITerm> tail = default)
        : this(ImmutableArray.CreateRange(contents), tail) { }

    public override Operator Operator => WellKnown.Operators.List;
    public override Atom EmptyElement => WellKnown.Literals.EmptyList;
    public override (string Open, string Close) Braces => ("[", "]");
    public override ITerm CanonicalForm { get; }
    protected override AbstractList Create(ImmutableArray<ITerm> contents) => new List(contents);

    public override IAbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        return new List(ImmutableArray.CreateRange(Contents.Select(c => c.Instantiate(ctx, vars))), Maybe.Some(Tail.Instantiate(ctx, vars)));
    }
    public override IAbstractTerm Substitute(Substitution s)
        => new List(ImmutableArray.CreateRange(Contents.Select(c => c.Substitute(s))), Maybe.Some(Tail.Substitute(s)));

    public override string Explain()
    {
        if (IsEmpty)
        {
            return Tail.Explain();
        }

        var joined = Contents.Join(t => t.Explain());
        if (!Tail.Equals(EmptyElement))
        {
            if (Tail.IsAbstract<List>().TryGetValue(out var rest))
            {
                joined = Contents.Select(t => t.Explain()).Append(rest.Explain()[1..^1]).Join();
                return $"{Braces.Open}{joined}{Braces.Close}";
            }

            return $"{Braces.Open}{joined}|{Tail.Explain()}{Braces.Close}";
        }

        return $"{Braces.Open}{joined}{Braces.Close}";
    }
    public static Maybe<List> FromCanonical(ITerm term) => Unfold(term, WellKnown.Literals.EmptyList, last => true, WellKnown.Functors.List)
        .Select(some => new List(some.SkipLast(1), some.Any() ? Maybe.Some(some.Last()) : default));
    public override Maybe<IAbstractTerm> FromCanonicalTerm(ITerm canonical) => FromCanonical(canonical).Select(x => (IAbstractTerm)x);
}
