using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface IAbstractTermParser<A> : IAbstractTermParser
    where A : AbstractTerm
{
    new Maybe<A> Parse(ErgoParser parser);
    Maybe<AbstractTerm> IAbstractTermParser.Parse(ErgoParser parser) => Parse(parser)
        .Map(Maybe.Some<AbstractTerm>);
}
