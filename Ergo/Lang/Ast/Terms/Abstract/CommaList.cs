using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class CommaList : AbstractList
{
    public static readonly CommaList Empty = new(ImmutableArray<ITerm>.Empty);

    public CommaList(ImmutableArray<ITerm> head)
        : base(head)
    {
        CanonicalForm = Fold(Functor, EmptyElement.WithAbstractForm(Maybe.Some<IAbstractTerm>(Empty)), head)
            .Reduce<ITerm>(a => a, v => v, c => c)
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this));
    }
    public CommaList(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents)) { }
    public override Atom Functor => WellKnown.Functors.CommaList.First();
    public override Atom EmptyElement => WellKnown.Literals.EmptyCommaList;
    public override (string Open, string Close) Braces => ("(", ")");
    public override ITerm CanonicalForm { get; }

    public static bool TryUnfold(ITerm term, out IEnumerable<ITerm> unfolded)
    {
        if (Unfold(term) is { HasValue: true } u)
        {
            unfolded = u.GetOrThrow();
            return true;
        }

        unfolded = default;
        return false;
    }
    public static Maybe<IEnumerable<ITerm>> Unfold(ITerm term) => Unfold(term, WellKnown.Operators.Conjunction.Synonyms);
    protected override AbstractList Create(ImmutableArray<ITerm> head) => new CommaList(head);
}
