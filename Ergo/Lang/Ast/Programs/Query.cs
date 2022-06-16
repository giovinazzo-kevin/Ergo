using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Goals.Explain() }")]
public readonly struct Query
{
    public readonly NTuple Goals;

    public Query(NTuple goals) => Goals = goals;
    public Query(params ITerm[] goals) => Goals = new(goals);
}
