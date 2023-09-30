using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Utils;

namespace Ergo.Lang.Parser;

public interface IAbstractTermParser<A> : IAbstractTermParser
    where A : IAbstractTerm
{
    void Register(AbstractTermCache cache);
    new Maybe<A> Parse(ErgoParser parser);
    Maybe<IAbstractTerm> IAbstractTermParser.Parse(ErgoParser parser) => Parse(parser)
        .Map(some => Maybe.Some<IAbstractTerm>(some));
}
