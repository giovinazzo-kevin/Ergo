using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class List : AbstractList
{
    public static readonly List Empty = new(ImmutableArray<ITerm>.Empty);
    public readonly ITerm Tail;
    public List(ImmutableArray<ITerm> contents, Maybe<ITerm> tail = default)
        : base(contents)
    {
        Tail = tail.Reduce(
            some => some,
            () => EmptyElement.WithAbstractForm(Maybe.Some<IAbstractTerm>(Empty)));
        CanonicalForm = Fold(Functor, Tail, contents)
            .Reduce<ITerm>(a => a, v => v, c => c)
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this));
    }
    public List(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents), default) { }

    public override Atom Functor => WellKnown.Functors.List.First();
    public override Atom EmptyElement => WellKnown.Literals.EmptyList;
    public override (string Open, string Close) Braces => ("[", "]");
    public override ITerm CanonicalForm { get; }
    protected override AbstractList Create(ImmutableArray<ITerm> contents) => new List(contents);

    public override string Explain()
    {
        if (IsEmpty)
        {
            return Tail.WithAbstractForm(default).Explain();
        }

        var joined = string.Join(',', Contents.Select(t => t.Explain()));
        if (!Tail.Equals(EmptyElement))
        {
            return $"{Braces.Open}{joined}|{Tail.WithAbstractForm(default).Explain()}{Braces.Close}";
        }

        return $"{Braces.Open}{joined}{Braces.Close}";
    }
}
