namespace Ergo.Lang.Parser;

public sealed class TupleParser : AbstractListParser<NTuple>
{
    protected override NTuple Construct(ImmutableArray<ITerm> seq) => new(seq);
}