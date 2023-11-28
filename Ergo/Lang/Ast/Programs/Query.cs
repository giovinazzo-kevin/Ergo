using Ergo.Lang.Compiler;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Goals.Explain() }")]
public class Query
{
    public readonly NTuple Goals;
    public Query(NTuple goals) => Goals = goals;
    public Query(params ITerm[] goals) => Goals = new(goals, default);
    public Query(ImmutableArray<ITerm> goals) => Goals = new(goals, default);
    public ErgoVM.Op Compile() => ErgoVM.Ops.Goals(Goals);
}
