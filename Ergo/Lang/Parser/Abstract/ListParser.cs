
namespace Ergo.Lang.Parser;

public sealed class ListParser<L> : AbstractTermParser<L>
    where L : AbstractList
{
    private readonly Func<ImmutableArray<ITerm>, Maybe<ITerm>, L> Constructor;

    public ListParser(Func<ImmutableArray<ITerm>, Maybe<ITerm>, L> construct) => Constructor = construct;

    public override Maybe<L> TryParse(ErgoParser parser)
    {
        var empty = Constructor(ImmutableArray<ITerm>.Empty, default);
        if (parser.TryParseSequence(
              empty.Functor
            , empty.EmptyElement
            , () => parser.TryParseTermOrExpression(out var t, out var p) ? (true, t, p) : (false, default, p)
            , empty.Braces.Open, WellKnown.Operators.Conjunction, empty.Braces.Close
            , true
            , out var full
        ))
        {
            if (full.Contents.Length == 1 && full.Contents[0] is Complex cplx
                && WellKnown.Functors.HeadTail.Contains(cplx.Functor))
            {
                var arguments = ImmutableArray<ITerm>.Empty.Add(cplx.Arguments[0]);
                arguments = CommaList.Unfold(cplx.Arguments[0])
                    .Reduce(some => ImmutableArray.CreateRange(some), () => arguments);
                return Maybe.Some(Constructor(arguments, Maybe.Some(cplx.Arguments[1])));
            }

            return Maybe.Some(Constructor(full.Contents, default));
        }

        if (parser.TryParseAtom(out var a) && a.Equals(empty.EmptyElement))
        {
            return Maybe.Some(Constructor(ImmutableArray<ITerm>.Empty, default));
        }

        return default;
    }
}
