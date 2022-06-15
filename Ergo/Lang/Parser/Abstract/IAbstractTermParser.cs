using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser.Abstract;

public interface IAbstractTermParser
{
    Maybe<IAbstractTerm> TryParse(ErgoParser parser);
}
