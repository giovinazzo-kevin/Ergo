using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Goals.Explain() }")]
public readonly struct Query
{
    public readonly CommaSequence Goals;

    public Query(CommaSequence goals) => Goals = goals;
}
