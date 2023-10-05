namespace Ergo.Lang.Parser;

public sealed class SetParser : AbstractListParser<Set>
{
    private Atom[] _functors;
    public override IEnumerable<Atom> FunctorsToIndex => _functors;

    public SetParser()
    {
        var emptyElem = Construct(ImmutableArray<ITerm>.Empty);
        _functors = emptyElem.Operator.Synonyms.Append((Atom)emptyElem.CanonicalForm).ToArray();
    }

    protected override Set Construct(ImmutableArray<ITerm> seq) => new(seq);
}
