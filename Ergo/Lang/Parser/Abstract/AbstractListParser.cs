namespace Ergo.Lang.Parser;

public abstract class AbstractListParser<L> : IAbstractTermParser<L>
    where L : AbstractList
{
    public virtual int ParsePriority => 0;

    protected L Empty { get; private set; }

    protected abstract L Construct(ImmutableArray<ITerm> seq, Maybe<ParserScope> scope);
    protected virtual L Merge(L abs, ITerm newArg, Maybe<ParserScope> scope)
    {
        return Construct(abs.Contents.Insert(0, newArg), scope);
    }

    protected virtual Maybe<L> ParseTail(Complex c)
    {
        return AbstractList
            .Unfold(c, Empty.EmptyElement, t => !t.Equals(Empty.EmptyElement), Empty.Operator.Synonyms)
            .Select(x => Construct(x.ToImmutableArray(), c.Scope))
            ;
    }

    protected virtual Maybe<L> FromCanonical(Complex c)
    {
        if (c.Arguments[1].Equals(Empty.EmptyElement))
        {
            return Construct(ImmutableArray.Create(c.Arguments[0]), c.Scope);
        }
        else if (c.Arguments[1] is L abs)
        {
            // this is eliding list tails
            return Merge(abs, c.Arguments[0], c.Scope);
        }
        return ParseTail(c);
    }

    public virtual Maybe<L> Parse(ErgoParser parser)
    {
        Empty = Construct(ImmutableArray<ITerm>.Empty, default);
        var scope = parser.GetScope();
        // Canonical list: a [|] b [|] []
        return ParseCanonical()
        // Sugared list: [a, b]
            .Or(ParseSugared)
            .Or(() => parser.MemoizeFailureAndFail<L>(scope.LexerState));

        Maybe<L> ParseCanonical() => parser.Complex()
            .Where(a => a.Arity == 2 && Empty.Operator.Synonyms.Contains(a.Functor))
            .Map(FromCanonical);
        Maybe<L> ParseSugared()
        {
            var ret = parser.Sequence(
                  Empty.Operator
                , Empty.EmptyElement
                , (Empty.Braces.Open, Empty.Braces.Close)
                , WellKnown.Operators.Conjunction)
                .Select(seq => Construct(seq.Contents, scope))
                .Or(() => parser.Atom()
                    .Where(a => a.Equals(Empty.EmptyElement))
                    .Select(_ => Empty));
            return ret;
        }
    }
}
