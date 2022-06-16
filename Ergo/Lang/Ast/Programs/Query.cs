using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Goals.Explain() }")]
public readonly struct Query
{
    public readonly Tuple Goals;

    public Query(Tuple goals) => Goals = goals;
    public Query(params ITerm[] goals) => Goals = new(goals);
}
