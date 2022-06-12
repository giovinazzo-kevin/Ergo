using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly struct Directive : IExplainable
{
    public readonly ITerm Body;

    public Directive(ITerm body)
    {
        Body = body;
    }

    public Directive WithBody(ITerm newBody) => new(newBody);

    public string Explain(bool canonical) => $"← {Body.Explain(canonical)}.";
}
