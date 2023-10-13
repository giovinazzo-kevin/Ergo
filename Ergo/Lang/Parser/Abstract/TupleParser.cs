namespace Ergo.Lang.Parser;

public sealed class TupleParser : AbstractListParser<NTuple>
{
    private Atom[] _functors;
    public override IEnumerable<Atom> FunctorsToIndex => _functors;

    public TupleParser()
    {
        var emptyElem = Construct(ImmutableArray<ITerm>.Empty, default);
        _functors = emptyElem.Operator.Synonyms.Append(emptyElem.EmptyElement).ToArray();
    }

    protected override NTuple Construct(ImmutableArray<ITerm> seq, Maybe<ParserScope> scope) => new(seq, scope);
}