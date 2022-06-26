namespace Ergo.Lang.Parser;

public abstract class AbstractListParser<L> : IAbstractTermParser<L>
    where L : AbstractList
{
    protected abstract L Construct(ImmutableArray<ITerm> seq);

    public Maybe<L> Parse(ErgoParser parser)
    {
        var empty = Construct(ImmutableArray<ITerm>.Empty);
        return parser.Sequence(
              empty.Functor
            , empty.EmptyElement
            , () => parser.TermOrExpression()
            , empty.Braces.Open, WellKnown.Operators.Conjunction, empty.Braces.Close
            , false)
            .Map(seq => Maybe.Some(seq)
                .Select(seq => seq.Contents.Length == 1 && seq.Contents[0] is Complex cplx && cplx.IsAbstract<NTuple>(out var tuple)
                    ? tuple.Contents : seq.Contents)
                .Select(seq => Construct(seq)))
            .Or(() => parser.Atom()
                .Where(a => a.Equals(empty.EmptyElement))
                .Select(_ => empty));
    }
}
