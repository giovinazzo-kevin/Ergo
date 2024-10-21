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

public class BuildExecutionGraphStep(ICompilePredicatePipeline compilePredicate, ICompileClauseStep compileClause) : IBuildExecutionGraphStep
{
    internal class CompileEnv(ErgoDependencyGraph depGraph, ErgoExecutionGraph execGraph) : ICompilePredicatePipeline.Env
    {
        public InstantiationContext InstantiationContext { get; } = new("$");
        public ErgoDependencyGraph DependencyGraph { get; } = depGraph;
        public ErgoExecutionGraph ExecutionGraph { get; } = execGraph;
    }

    public Either<ErgoExecutionGraph, PipelineError> Run(ErgoDependencyGraph depGraph, IBuildExecutionGraphStep.Env env)
    {
        var execGraph = new ErgoExecutionGraph();
        var compileEnv = new CompileEnv(depGraph, execGraph);
        // Compile clauses in order of max dependency depth.
        // This ensures that all dependencies of a clause are
        // available and compiled by the time they are needed.
        var sortedClauses = depGraph.Predicates.Values
            .SelectMany(x => x.Clauses
                .Select(y => (Predicate: x, Clause: y))
            .OrderBy(x => x.Clause.DependencyDepth));
        var dict = new Dictionary<PredicateDefinition, List<ClauseNode>>();
        foreach (var (pred, clause) in sortedClauses)
        {
            if (!dict.TryGetValue(pred, out var clauses))
                clauses = dict[pred] = [];
            var compileResult = compileClause.Run(clause, compileEnv);
            if (compileResult.TryGetB(out var error))
                return error;
            clauses.Add(compileResult.GetAOrThrow());
        }
        return execGraph;
    }

}

