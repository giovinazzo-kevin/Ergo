using Ergo.Solver;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Goals.Explain() }")]
public class Query
{
    public readonly NTuple Goals;

    /// <summary>
    /// Analyzes the query and caches all relevant structures in the SolverScope.
    /// </summary>
    public void Compile(KnowledgeBase kb, ref SolverScope scope)
    {

    }

    public Query(NTuple goals) => Goals = goals;
    public Query(params ITerm[] goals) => Goals = new(goals, default);
}
