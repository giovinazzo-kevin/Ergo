namespace Ergo.Lang.Parser;

public abstract class AbstractListParser<L> : IAbstractTermParser<L>
    where L : AbstractList
{
    protected abstract L Construct(ImmutableArray<ITerm> seq);

    public Maybe<L> Parse(ErgoParser parser)
    {
        var empty = Construct(ImmutableArray<ITerm>.Empty);
        //parser = parser.Facade
        //    .RemoveAbstractParser<NTuple>()
        //    .BuildParser(parser.Lexer.Stream, parser.Lexer.AvailableOperators);
        var ret = parser.Sequence2(
              empty.Functor
            , empty.EmptyElement
            , empty.Braces.Open
            , WellKnown.Operators.Conjunction
            , empty.Braces.Close)
            //.Map(seq => Maybe.Some(seq)
            //    .Where(seq => seq.Contents.Length == 1)
            //    .Map(seq => NTuple.FromPseudoCanonical(seq.Contents.Single(), Maybe.Some(false), Maybe.Some(false)))
            //    .Select(tup => tup.Contents)
            //    .Or(() => seq.Contents))
            .Select(seq => Construct(seq.Contents))
            .Or(() => parser.Atom()
                .Where(a => a.Equals(empty.EmptyElement))
                .Select(_ => empty));
        return ret;
    }
}
