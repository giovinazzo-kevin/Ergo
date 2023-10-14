using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface AbstractTermParser
{
    int ParsePriority { get; }
    Maybe<AbstractTerm> Parse(ErgoParser parser);
}
