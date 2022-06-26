using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class List : AbstractList
{
    public static readonly List Empty = new(ImmutableArray<ITerm>.Empty);
    public readonly ITerm Tail;
    public List(ImmutableArray<ITerm> contents, Maybe<ITerm> tail = default)
        : base(contents)
    {
        Tail = tail.GetOr((ITerm)EmptyElement.WithAbstractForm(Empty ?? this));
        CanonicalForm = Fold(Functor, Tail, contents)
            .Reduce<ITerm>(a => a, v => v, c => c)
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this));
    }
    public List(IEnumerable<ITerm> contents, Maybe<ITerm> tail = default)
        : this(ImmutableArray.CreateRange(contents), tail) { }

    public override Atom Functor => WellKnown.Functors.List.First();
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
            return Tail.WithAbstractForm(default).Explain();
        }

        var joined = Contents.Join(t => t.Explain());
        if (!Tail.Equals(EmptyElement))
        {
            if (Tail.IsAbstract<List>(out var rest))
            {
                joined = Contents.Select(t => t.Explain()).Append(rest.Explain()[1..^1]).Join();
                return $"{Braces.Open}{joined}{Braces.Close}";
            }

            return $"{Braces.Open}{joined}|{Tail.WithAbstractForm(default).Explain()}{Braces.Close}";
        }

        return $"{Braces.Open}{joined}{Braces.Close}";
    }
    public static Maybe<List> FromCanonical(ITerm term) => Unfold(term, last => true, WellKnown.Functors.List)
        .Select(some => new List(some.SkipLast(1), Maybe.Some(some.Last())));
}
