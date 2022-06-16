namespace Ergo.Lang.Ast;

public sealed class CommaList : AbstractList
{
    public static CommaList Empty => new(ImmutableArray<ITerm>.Empty);

    public CommaList(ImmutableArray<ITerm> head, Maybe<ITerm> tail = default)
        : base(head, tail) { }
    public CommaList(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents), default) { }
    public override Atom Functor => WellKnown.Functors.CommaList.First();
    public override Atom EmptyElement => WellKnown.Literals.EmptyCommaList;
    public override (string Open, string Close) Braces => ("(", ")");

    public static Maybe<IEnumerable<ITerm>> TryUnfold(ITerm term)
    {
        if (term is Complex { Arity: 2, Functor: var f } c && WellKnown.Operators.Conjunction.Synonyms.Contains(f))
            return Maybe.Some(Inner());
        return default;

        IEnumerable<ITerm> Inner()
        {
            while (term is Complex { Arity: 2, Functor: var f } c && WellKnown.Operators.Conjunction.Synonyms.Contains(f))
            {
                yield return c.Arguments[0];
                term = c.Arguments[1];
            }

            yield return term;
        }
    }
}
