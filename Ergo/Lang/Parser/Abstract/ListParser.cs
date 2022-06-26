namespace Ergo.Lang.Parser;

public sealed class ListParser : AbstractListParser<List>
{
    protected override List Construct(ImmutableArray<ITerm> seq)
    {
        if (seq.Length == 1 && seq[0] is Complex cplx)
        {
            if (WellKnown.Functors.HeadTail.Contains(cplx.Functor))
            {
                var arguments = ImmutableArray<ITerm>.Empty.Add(cplx.Arguments[0]);
                arguments = NTuple.FromQuasiCanonical(cplx.Arguments[0], Maybe.Some(false), Maybe.Some(false)) is { HasValue: true } tup
                    ? tup.GetOrThrow().Contents : arguments;
                return new List(arguments, Maybe.Some(cplx.Arguments[1]));
            }
        }

        return new List(seq);
    }
}
