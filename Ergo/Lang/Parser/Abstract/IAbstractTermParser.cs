using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface IAbstractTermParser
{
    Maybe<IAbstractTerm> Parse(ErgoParser parser);
}
