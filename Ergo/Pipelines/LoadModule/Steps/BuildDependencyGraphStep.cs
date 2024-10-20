using Ergo.Compiler;
using Ergo.Modules;
using static Ergo.Lang.Ast.WellKnown;

namespace Ergo;

public interface IBuildDependencyGraphStep : IErgoPipeline<ErgoModuleTree, ErgoDependencyGraph, IBuildDependencyGraphStep.Env>
{
    public interface Env
    {
    }
}

public class BuildDependencyGraphStep : IBuildDependencyGraphStep
{
    public Either<ErgoDependencyGraph, PipelineError> Run(ErgoModuleTree moduleTree, IBuildDependencyGraphStep.Env env)
    {
        var graph = new ErgoDependencyGraph();
        foreach (var builtIn in moduleTree.BuiltIns.Values)
        {
            graph.Predicates[builtIn.Signature]
                = graph.Predicates[builtIn.Signature.WithModule(default)]
                = new ErgoDependencyGraph.PredicateDefinition([], [], builtIn);
        }
        var queue = new Queue<(ErgoDependencyGraph.PredicateDefinition PredicateDef, ErgoDependencyGraph.ClauseDefinition ClauseDef, ITerm Statement)>();
        foreach (var module in moduleTree.Modules.Values.OrderBy(x => x.LoadOrder))
        {
            var signatures = module.Clauses.Select(x => x.Head.GetSignature()).ToHashSet();
            foreach (var clause in module.Clauses)
            {
                var sig = clause.Head.GetSignature();
                if (!graph.Predicates.TryGetValue(sig, out var pred))
                    graph.Predicates[sig] = pred = new([], [], default);
                var meta = module.GetMetaTableEntry(sig);
                pred.Clauses.Add(new (
                    clause.Head,
                    clause.Body,
                    module.Name,
                    sig.Module,
                    clause.IsFactual,
                    clause.IsTailRecursive,
                    meta.IsExported,
                    meta.IsDynamic
                ));
                if (meta.IsExported)
                    graph.Predicates[sig.WithModule(default)] = pred;
                foreach (var statement in clause.Body.Contents)
                    queue.Enqueue((pred, pred.Clauses[^1], statement));
            }
            while (queue.TryDequeue(out var item))
            {
                if (item.Statement is Variable)
                    continue;
                var sig = item.Statement.GetSignature();
                var variadicSig = sig.WithArity(default);
                if (graph.Predicates.TryGetValue(sig, out var def) || graph.Predicates.TryGetValue(variadicSig, out def))
                    item.PredicateDef.Dependencies.Add(def);
                else
                {
                    if (signatures.Contains(sig)) // local definition
                        queue.Enqueue(item);
                    else
                        throw new CompilerException(Lang.Compiler.ErgoCompiler.ErrorType.UnresolvedPredicate, sig.Explain());
                }
            }
        }
        return graph;
    }
}

