using Ergo.Facade;
using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Tests;

public class SimpleErgoTests
{
    public readonly ErgoFacade Facade;

    public SimpleErgoTests() { Facade = ErgoFacade.Standard; }
    // "⊤" : "⊥"
    protected void ShouldParse<T>(string query, T expected)
    {
        var parsed = new Parsed<T>(Facade, query, _ => default, Array.Empty<Operator>())
            .Value.GetOrThrow(new InvalidOperationException($"Could not parse: {query}"));
        if (parsed is IExplainable expl && expected is IExplainable expExpl)
            Assert.Equal(expl.Explain(true), expExpl.Explain(true));
        else Assert.Equal(parsed, expected);
    }
    // "⊤" : "⊥"
    protected void ShouldNotParse<T>(string query, T expected)
    {
        var parsed = new Parsed<T>(Facade, query, _ => default, Array.Empty<Operator>())
            .Value;
        if (parsed.TryGetValue(out var value))
        {
            Assert.NotEqual(value, expected);
        }
    }
}
