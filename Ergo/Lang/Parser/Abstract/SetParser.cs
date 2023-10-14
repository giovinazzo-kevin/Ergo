namespace Ergo.Lang.Parser;

public sealed class SetParser : AbstractListParser<Set>
{
    protected override Set Construct(ImmutableArray<ITerm> seq, Maybe<ParserScope> scope) => new(seq, scope, false);
}
