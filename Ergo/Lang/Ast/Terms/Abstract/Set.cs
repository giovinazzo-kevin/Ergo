using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class Set : AbstractList
{
    public static readonly Set Empty = new(ImmutableArray<ITerm>.Empty);

    public Set(ImmutableArray<ITerm> head)
        : base(head)
    {
        CanonicalForm = Fold(Functor, EmptyElement.WithAbstractForm(Maybe.Some<IAbstractTerm>(Empty)), ImmutableArray.CreateRange(head.OrderBy(x => x).Distinct()))
            .Reduce<ITerm>(a => a, v => v, c => c)
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this));
    }
    public Set(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents)) { }
    public override Atom Functor => WellKnown.Functors.BracyList.First();
    public override Atom EmptyElement => WellKnown.Literals.EmptyBracyList;
    public override (string Open, string Close) Braces => ("{", "}");
    public override ITerm CanonicalForm { get; }

    protected override AbstractList Create(ImmutableArray<ITerm> head) => new Set(head);
}
