using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface IAbstractTermParser
{
    int ParsePriority { get; }
    Maybe<AbstractTerm> Parse(ErgoParser parser);
}
