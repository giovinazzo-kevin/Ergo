namespace Ergo.Lang.Parser;

public sealed class SetParser : AbstractListParser<Set>
{
    protected override Set Construct(ImmutableArray<ITerm> seq) => new(seq);
}
