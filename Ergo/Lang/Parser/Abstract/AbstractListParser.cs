namespace Ergo.Lang.Parser;

public abstract class AbstractListParser<L> : IAbstractTermParser<L>
    where L : AbstractList
{
    public abstract IEnumerable<Atom> FunctorsToIndex { get; }
    protected abstract L Construct(ImmutableArray<ITerm> seq);

    public Maybe<L> Parse(ErgoParser parser)
    {
        var empty = Construct(ImmutableArray<ITerm>.Empty);
        var ret = parser.Sequence(
              empty.Operator
            , empty.EmptyElement
            , empty.Braces.Open
            , WellKnown.Operators.Conjunction
            , empty.Braces.Close)
            .Select(seq => Construct(seq.Contents))
            .Or(() => parser.Atom()
                .Where(a => a.Equals(empty.EmptyElement))
                .Select(_ => empty));
        return ret;
    }
}
