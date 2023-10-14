using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface AbstractTermParser<A> : AbstractTermParser
    where A : AbstractTerm
{
    new Maybe<A> Parse(ErgoParser parser);
    Maybe<AbstractTerm> AbstractTermParser.Parse(ErgoParser parser) => Parse(parser)
        .Map(Maybe.Some<AbstractTerm>);
}
