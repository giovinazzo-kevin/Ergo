using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Parser;

public interface IAbstractTermParser
{
    int ParsePriority { get; }
    IEnumerable<Atom> FunctorsToIndex { get; }
    Maybe<IAbstractTerm> Parse(ErgoParser parser);
}
