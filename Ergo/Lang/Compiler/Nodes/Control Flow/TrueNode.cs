using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class TrueNode : StaticNode
{
    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        yield return execScope.AsSolution();
    }
}
