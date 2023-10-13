namespace Ergo.Lang.Parser;

public sealed class SetParser : AbstractListParser<Set>
{
    private Atom[] _functors;
    public override IEnumerable<Atom> FunctorsToIndex => _functors;

    public SetParser()
    {
        var emptyElem = Construct(ImmutableArray<ITerm>.Empty, default);
        _functors = emptyElem.Operator.Synonyms.Append(emptyElem.EmptyElement).ToArray();
    }

    protected override Set Construct(ImmutableArray<ITerm> seq, Maybe<ParserScope> scope) => new(seq, scope);
}
