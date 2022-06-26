namespace Ergo.Lang.Parser;

public abstract class AbstractListParser<L> : IAbstractTermParser<L>
    where L : AbstractList
{
    protected abstract L Construct(ImmutableArray<ITerm> seq);

    public Maybe<L> TryParse(ErgoParser parser)
    {
        var empty = Construct(ImmutableArray<ITerm>.Empty);
        if (parser.TryParseSequence(
              empty.Functor
            , empty.EmptyElement
            , () => parser.TryParseTermOrExpression(out var t, out var p) ? (true, t, p) : (false, default, p)
            , empty.Braces.Open, WellKnown.Operators.Conjunction, empty.Braces.Close
            , false
            , out var seq
        ))
        {
            // Special case for tuples
            if (seq.Contents.Length == 1 && seq.Contents[0] is Complex cplx)
            {
                if (cplx.IsAbstract<NTuple>(out var tuple))
                {
                    return Construct(tuple.Contents);
                }
            }

            return Construct(seq.Contents);
        }

        if (parser.TryParseAtom(out var a) && a.Equals(empty.EmptyElement))
        {
            return empty;
        }

        return default;
    }
}
