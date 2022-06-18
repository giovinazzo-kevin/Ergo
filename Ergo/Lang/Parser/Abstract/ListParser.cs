using Ergo.Lang.Ast.Terms.Interfaces;

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
            if (full.Contents.Length == 1 && full.Contents[0] is Complex cplx)
            {
                if (cplx.IsAbstract<NTuple>(out var tuple))
                {
                    if (typeof(L) == typeof(NTuple))
                        return Maybe.Some((L)(IAbstractTerm)tuple);
                    return Maybe.Some(Constructor(tuple.Contents, default));
                }

                if (typeof(L) == typeof(List) && WellKnown.Functors.HeadTail.Contains(cplx.Functor))
                {
                    var arguments = ImmutableArray<ITerm>.Empty.Add(cplx.Arguments[0]);
                    arguments = NTuple.FromQuasiCanonical(cplx.Arguments[0], Maybe.Some(false), Maybe.Some(false)) is { HasValue: true } tup
                        ? tup.GetOrThrow().Contents : arguments;
                    return Maybe.Some(Constructor(arguments, Maybe.Some(cplx.Arguments[1])));
                }
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
