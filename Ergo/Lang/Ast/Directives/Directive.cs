using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain(false) }")]
public readonly struct Directive : IExplainable
{
    public readonly ITerm Body;
    public readonly string Documentation;
    public Directive(ITerm body, string doc) => (Body, Documentation) = (body, doc);

    public Directive WithBody(ITerm newBody, string newDoc = null) => new(newBody, newDoc ?? Documentation);

    public string Explain(bool canonical)
    {
        var doc = string.Join("\r\n", Documentation.Replace("\r", "").Split('\n').AsEnumerable().Select(r => "%: " + r));
        var expl = $"← {Body.Explain(canonical)}";
        if (!canonical && !string.IsNullOrWhiteSpace(Documentation))
        {
            expl = $"{doc}\r\n{expl}";
        }

        return $"{doc}\r\n{expl}.";
    }
}
