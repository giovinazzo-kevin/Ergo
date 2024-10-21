using Ergo.Compiler;
using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Modules;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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
        var graph = new ErgoDependencyGraph(moduleTree);
        foreach (var builtIn in moduleTree.BuiltIns.Values)
            graph.Predicates[builtIn.Signature] = graph.Predicates[builtIn.Signature.WithModule(default)] = new PredicateDefinition(
                builtIn.Signature.Module.GetOrThrow(),
                builtIn.Signature.Functor,
                builtIn.Signature.Arity,
                builtIn,
                [],
                IsExported: true,
                IsDynamic: false
            );
        var queue = new Queue<(PredicateDefinition Pred, ClauseDefinition Clause, GoalDefinition Goal)>();
        foreach (var module in moduleTree.Modules.Values.OrderBy(x => x.LoadOrder))
        {
            foreach (var (sig, meta) in module.MetaTable)
            {
                if (!meta.IsDynamic)
                    continue;
                if (!graph.Predicates.TryGetValue(sig, out var pred))
                    graph.Predicates[sig] = pred = new(
                        sig.Module.GetOrThrow(),
                        sig.Functor,
                        sig.Arity,
                        default,
                        [],
                        meta.IsExported,
                        meta.IsDynamic
                    );
                if (meta.IsExported)
                    graph.Predicates[sig.WithModule(default)] = pred;
            }
            var localSignatures = module.Clauses.Select(x => x.Head.GetSignature())
                .ToHashSet();
            foreach (var clause in module.Clauses)
            {
                var sig = clause.Head.Qualified(module.Name).GetSignature();
                var meta = module.GetMetaTableEntry(sig);
                if (!graph.Predicates.TryGetValue(sig, out var pred))
                    graph.Predicates[sig] = pred = new(
                        sig.Module.GetOrThrow(),
                        sig.Functor,
                        sig.Arity,
                        default,
                        [],
                        meta.IsExported,
                        meta.IsDynamic
                    );
                var clauseVariables = clause.Head.Variables
                    .Concat(clause.Body.Variables)
                    .Distinct()
                    .Select((v, i) => (v, i))
                    .ToDictionary(x => x.v, x => x.i);
                var args = clause.Head.GetArguments()
                    .Select(a => MapArg(a, clauseVariables));
                var goals = clause.Body.Contents
                    .Select<ITerm, GoalDefinition>(term => term is Variable 
                    ? new RuntimeGoalDefinition(MapArg(term, clauseVariables))
                    : new StaticGoalDefinition(term.GetSignature(), term.GetArguments()
                        .Select(a => MapArg(a, clauseVariables))
                        .ToArray()));
                pred.Clauses.Add(new(
                    Functor: sig.Functor,
                    Arity: sig.Arity.GetOrThrow(),
                    Args: [.. args],
                    Goals: [.. goals],
                    DeclaringModule: module.Name,
                    DeclaredModule: sig.Module,
                    IsGround: clause.Head.IsGround && clause.Body.IsGround,
                    IsFactual: clause.IsFactual,
                    IsTailRecursive: clause.IsTailRecursive,
                    Dependencies: []
                ));
                if (meta.IsExported)
                    graph.Predicates[sig.WithModule(default)] = pred;
                foreach (var goal in pred.Clauses[^1].Goals)
                    queue.Enqueue((pred, pred.Clauses[^1], goal));
            }
            while (queue.TryDequeue(out var item))
            {
                if (item.Goal is not StaticGoalDefinition { Signature: var sig } sGoal)
                    continue;
                // Resolve the goal by adding a dependency to the clause and setting sGoal's Callee
                var variadicSig = sig.WithArity(default);
                if(item.Clause.IsTailRecursive 
                    && (!sig.Module.HasValue || sig.Module.Check(x => x.Equals(item.Pred.Module)))
                    && sig.Functor.Equals(item.Pred.Functor)
                    && (!item.Pred.Arity.HasValue || item.Pred.Arity.Equals(sig.Arity)))
                    item.Clause.Dependencies.TryAdd(sig, sGoal.Callee = item.Pred);
                else if (graph.Predicates.TryGetValue(sig, out var def))
                    item.Clause.Dependencies.TryAdd(sig, sGoal.Callee = def);
                else if (graph.Predicates.TryGetValue(sig.WithModule(item.Clause.DeclaringModule), out def))
                    item.Clause.Dependencies.TryAdd(sig, sGoal.Callee = def);
                else if (graph.Predicates.TryGetValue(variadicSig, out def))
                    item.Clause.Dependencies.TryAdd(variadicSig, sGoal.Callee = def);
                else
                {
                    if (localSignatures.Contains(sig)) // local definition
                        queue.Enqueue(item);
                    else
                        throw new CompilerException(Lang.Compiler.ErgoCompiler.ErrorType.UnresolvedPredicate, sig.Explain());
                }
            }
        }
        foreach (var clause in graph.Predicates.Values.SelectMany(x => x.Clauses))
            (clause.DependencyDepth, clause.IsCyclical) = CalcDependencyDepthAndDetectCycle(clause);
        return graph;

        static (int DependencyDepth, bool IsCyclical) CalcDependencyDepthAndDetectCycle(
            ClauseDefinition clause,
            HashSet<ClauseDefinition> visited = null,
            int depth = 0)
        {
            // Base cases: if the clause is factual, or if a cycle is detected
            if (clause.IsFactual)
                return (depth, false);
            // If already calculated, return stored dependency depth
            if (clause.DependencyDepth != 0)
                return (depth + clause.DependencyDepth, clause.IsCyclical);
            if ((visited ??= new HashSet<ClauseDefinition>()).Contains(clause))
                return (depth, true); // Cycle detected, return current depth and cycle flag
            visited.Add(clause);
            var inner =
                clause.Dependencies.Values
                .SelectMany(dep => dep.Clauses)
                .Select(cl => CalcDependencyDepthAndDetectCycle(cl, visited, depth + 1))
                .ToList();
            // Calculate maximum dependency depth across all dependencies
            clause.DependencyDepth = inner
                .DefaultIfEmpty((DependencyDepth: 0, false))
                .Max(result => result.DependencyDepth);
            clause.IsCyclical = inner.Any(x => x.IsCyclical);
            return (depth + clause.DependencyDepth, clause.IsCyclical);
        }

        static ArgDefinition MapArg(ITerm arg, Dictionary<Variable, int> clauseVars)
        {
            return arg switch
            {
                Atom a => new ConstArgDefinition(a),
                Variable v => new VariableArgDefinition(clauseVars[v]),
                Complex c => new ComplexArgDefinition(c.Functor, [.. c.Arguments.Select(arg => MapArg(arg, clauseVars))]),
                AbstractTerm b => MapArg(b.CanonicalForm, clauseVars),
                _ => throw new NotSupportedException()
            };
        }
    }
}

