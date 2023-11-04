using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class CutNode : StaticNode
{
    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        // Clear the stack to prevent further backtracking
        yield return execScope.Cut().Now(this);
    }
    public override string Explain(bool canonical = false) => $"!";
}
