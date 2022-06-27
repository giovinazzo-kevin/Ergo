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
                .Where(seq => seq.Contents.Length == 1 && seq.Contents[0] is Complex)
                .Map(seq => seq.Contents[0].IsAbstract<NTuple>()
                    .Select(tup => tup.Contents))
                .Or(() => seq.Contents)
                .Select(seq => Construct(seq)))
            .Or(() => parser.Atom()
                .Where(a => a.Equals(empty.EmptyElement))
                .Select(_ => empty));
    }
}
