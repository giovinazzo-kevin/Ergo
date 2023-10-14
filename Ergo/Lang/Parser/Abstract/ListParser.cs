namespace Ergo.Lang.Parser;

public sealed class ListParser : AbstractListParser<List>
{
    protected override List Merge(List abs, ITerm newArg, Maybe<ParserScope> scope)
    {
        if (!abs.Tail.Equals(WellKnown.Literals.EmptyList))
        {
            return new List(abs.Contents.Prepend(newArg).ToImmutableArray(), Maybe.Some(abs.Tail), scope);
        }
        return base.Merge(abs, newArg, scope);
    }

    protected override Maybe<List> ParseTail(Complex c)
    {
        if (!c.Arguments[1].Equals(WellKnown.Literals.EmptyList)
            && c.Arguments[1] is not List)
        {
            return new List(ImmutableArray.Create(c.Arguments[0]), Maybe.Some(c.Arguments[1]));
        }
        return AbstractList
            .Unfold(c, Empty.EmptyElement, t => !t.Equals(Empty.EmptyElement), Empty.Operator.Synonyms)
            .Select(x => Construct(x.ToImmutableArray(), c.Scope))
            ;
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
                return new List(arguments, Maybe.Some(cplx.Arguments[1]), scope, false);
            }
        }

        return new List(seq, default, scope, false);
    }
}
