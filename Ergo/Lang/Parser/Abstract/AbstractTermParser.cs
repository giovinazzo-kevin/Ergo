using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface IAbstractTermParser<A> : IAbstractTermParser
    where A : IAbstractTerm
{
    new Maybe<A> TryParse(ErgoParser parser);
    Maybe<IAbstractTerm> IAbstractTermParser.TryParse(ErgoParser parser) => TryParse(parser)
        .Map<IAbstractTerm>(some => some);
}
