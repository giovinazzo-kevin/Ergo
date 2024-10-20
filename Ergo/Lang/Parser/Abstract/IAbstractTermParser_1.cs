using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface IAbstractTermParser<A> : IAbstractTermParser
    where A : AbstractTerm
{
    Type IAbstractTermParser.Type => typeof(A);
    new Maybe<A> Parse(LegacyErgoParser parser);
    Maybe<AbstractTerm> IAbstractTermParser.Parse(LegacyErgoParser parser) => Parse(parser)
        .Map(Maybe.Some<AbstractTerm>);
}
