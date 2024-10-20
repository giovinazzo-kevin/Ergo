using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface IAbstractTermParser
{
    int ParsePriority { get; }
    Type Type { get; }
    Maybe<AbstractTerm> Parse(LegacyErgoParser parser);
}
