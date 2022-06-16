using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public abstract class AbstractTermParser<A> : IAbstractTermParser
    where A : IAbstractTerm
{
    public abstract Maybe<A> TryParse(ErgoParser parser);
    Maybe<IAbstractTerm> IAbstractTermParser.TryParse(ErgoParser parser) => TryParse(parser)
        .Map<IAbstractTerm>(some => some);
}
