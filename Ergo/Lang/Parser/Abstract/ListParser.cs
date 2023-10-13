namespace Ergo.Lang.Parser;

public sealed class ListParser : AbstractListParser<List>
{
    private Atom[] _functors;
    public override IEnumerable<Atom> FunctorsToIndex => _functors;

    public ListParser()
    {
        var emptyElem = Construct(ImmutableArray<ITerm>.Empty, default);
        _functors = emptyElem.Operator.Synonyms.Append(emptyElem.EmptyElement).ToArray();
    }

    protected override List Construct(ImmutableArray<ITerm> seq, Maybe<ParserScope> scope)
    {
        if (seq.Length == 1 && seq[0] is Complex cplx)
        {
            if (WellKnown.Functors.HeadTail.Contains(cplx.Functor))
            {
                var arguments = ImmutableArray<ITerm>.Empty.Add(cplx.Arguments[0]);
                arguments = NTuple.FromPseudoCanonical(cplx.Arguments[0], scope, false, false)
                    .Select(x => x.Contents)
                    .GetOr(arguments);
                return new List(arguments, Maybe.Some(cplx.Arguments[1]), scope);
            }
        }

        return new List(seq, default, scope);
    }
}
