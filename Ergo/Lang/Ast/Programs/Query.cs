using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Goals.Explain() }")]
public readonly struct Query
{
    public readonly CommaList Goals;

    public Query(CommaList goals) => Goals = goals;
    public Query(params ITerm[] goals) => Goals = new(goals);
}
