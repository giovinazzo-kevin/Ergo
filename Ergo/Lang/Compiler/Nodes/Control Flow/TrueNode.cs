using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class TrueNode : StaticNode
{
    public static readonly TrueNode Instance = new();

    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        yield return execScope.AsSolution();
    }
    public override string Explain(bool canonical = false) => $"⊤";
}
