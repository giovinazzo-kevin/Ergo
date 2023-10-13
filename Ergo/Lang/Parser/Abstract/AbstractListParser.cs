namespace Ergo.Lang.Parser;

public abstract class AbstractListParser<L> : AbstractTermParser<L>
    where L : AbstractList
{
    public virtual int ParsePriority => 0;
    public abstract IEnumerable<Atom> FunctorsToIndex { get; }
    protected abstract L Construct(ImmutableArray<ITerm> seq, Maybe<ParserScope> scope);

    public Maybe<L> Parse(ErgoParser parser)
    {
        var scope = parser.GetScope();
        var empty = Construct(ImmutableArray<ITerm>.Empty, scope);
        var ret = parser.Sequence(
              empty.Operator
            , empty.EmptyElement
            , empty.Braces.Open
            , WellKnown.Operators.Conjunction
            , empty.Braces.Close)
            .Select(seq => Construct(seq.Contents, scope))
            .Or(() => parser.Atom()
                .Where(a => a.Equals(empty.EmptyElement))
                .Select(_ => empty));
        return ret;
    }
}
