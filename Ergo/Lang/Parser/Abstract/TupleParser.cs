namespace Ergo.Lang.Parser;

public sealed class TupleParser : AbstractListParser<NTuple>
{
    private Atom[] _functors;
    public override IEnumerable<Atom> FunctorsToIndex => _functors;

    public TupleParser()
    {
        var emptyElem = Construct(ImmutableArray<ITerm>.Empty);
        _functors = emptyElem.Operator.Synonyms.Append((Atom)emptyElem.CanonicalForm).ToArray();
    }

    protected override NTuple Construct(ImmutableArray<ITerm> seq) => new(seq);
}