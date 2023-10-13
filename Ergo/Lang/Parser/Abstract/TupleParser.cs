namespace Ergo.Lang.Parser;

public sealed class TupleParser : AbstractListParser<NTuple>
{
    private Maybe<NTuple> _ParseArgList(ErgoParser parser) => base.Parse(parser)
        ;
    public override Maybe<NTuple> Parse(ErgoParser parser) => _ParseArgList(parser)
        .Where(x => x.Contents.Length != 1)
        ;
    public static Maybe<NTuple> ParseArgList(ErgoParser parser) =>
        new TupleParser()
            ._ParseArgList(parser)
        ;

    protected override NTuple Construct(ImmutableArray<ITerm> seq, Maybe<ParserScope> scope) => new(seq, scope);
}