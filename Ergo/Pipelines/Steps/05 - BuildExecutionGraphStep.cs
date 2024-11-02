using Ergo.Compiler;
using Ergo.Lang.Ast;
using Ergo.Lang.Compiler;
using Ergo.Pipelines;

namespace Ergo;

public interface IBuildExecutionGraphStep : IErgoPipeline<ErgoDependencyGraph, ErgoExecutionGraph, IBuildExecutionGraphStep.Env>
{
    public interface Env
    {
    }
}

public class BuildExecutionGraphStep(ICompileClauseStep compileClause) : IBuildExecutionGraphStep
{
    internal class CompileEnv(ErgoDependencyGraph depGraph, ErgoExecutionGraph execGraph) : ICompileClauseStep.Env
    {
        public InstantiationContext InstantiationContext { get; } = new("$");
        public ErgoDependencyGraph DependencyGraph { get; } = depGraph;
        public ErgoExecutionGraph ExecutionGraph { get; } = execGraph;
        public ErgoMemory Memory { get; } = new();
        public Dictionary<int, int> VarMap { get; } = [];
    }

    public Either<ErgoExecutionGraph, PipelineError> Run(ErgoDependencyGraph depGraph, IBuildExecutionGraphStep.Env env)
    {
        var execGraph = new ErgoExecutionGraph();
        var compileEnv = new CompileEnv(depGraph, execGraph);
        foreach (var (_, pred) in depGraph.Predicates)
        {
            var builtIn = pred.BuiltIn
                .Select(some => {
                    var headAddr = compileEnv.Memory.StoreHead(some);
                    return new BuiltInNode(headAddr, some);
                });
            execGraph.Declare(pred, pred.Clauses.Count, builtIn);
        }
        var clauses = depGraph.Predicates.Values
            .SelectMany(x => x.Clauses
                .Select((y, i) => (Pred: x, Clause: y, i)));
        foreach (var (pred, clause, i) in clauses)
        {
            var compileResult = compileClause.Run(clause, compileEnv);
            if (compileResult.TryGetB(out var error))
                return error;
            execGraph[pred].Clauses[i] = compileResult.GetAOrThrow();
        }
        return execGraph;
    }

}

