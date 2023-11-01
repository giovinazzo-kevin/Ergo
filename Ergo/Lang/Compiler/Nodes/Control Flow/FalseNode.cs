using Ergo.Solver;

namespace Ergo.Lang.Compiler;

public class FalseNode : StaticNode
{
    public static readonly FalseNode Instance = new();

    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        yield break;
    }

    public override string Explain(bool canonical = false) => "⊥";
}
