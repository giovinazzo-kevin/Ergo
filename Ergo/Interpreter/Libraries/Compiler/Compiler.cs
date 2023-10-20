using Ergo.Events;
using Ergo.Events.Solver;
using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.Compiler;

using System.Collections.Generic;

public class Compiler : Library
{
    public class CompilerNode : DependencyGraphNode
    {
        public bool IsInlined { get; set; }
        public bool IsCyclical { get; set; }
        public Predicate InlinedPredicate { get; set; }

    }

    public override int LoadOrder => 100;

    public Compiler()
    {

    }

    protected readonly HashSet<Signature> InlinedPredicates = new();
    public override Atom Module => WellKnown.Modules.Compiler;
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() { yield break; }
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() { yield break; }
    public void AddInlinedPredicate(Signature sig)
    {
        InlinedPredicates.Add(sig);
    }

    public override void OnErgoEvent(ErgoEvent evt)
    {
        if (evt is SolverInitializingEvent sie)
        {
            // This library reacts last to ErgoEvents (in a standard environment), so all predicates
            // that have been marked for inlining are known by now. It's time for some static analysis.
            var graph = new DependencyGraph<CompilerNode>();
            // The concept is similar to term expansions, but instead of recursively expanding each term,
            // inlining works at the goal level, allowing it to be qualified with a module and thus be more specific.
            // For example, you can inline prologue:(=)/2 without inlining dict:(=)/2 even though they share a signature.
            // This is because while we can still reference dict:(=)/2 unambiguously by its module, producing:
            // (unify(A, B) ; dict:(=)(A, B))
            // This allows us to be selective in how goals are expanded and to only inline when it improves performance.
            graph.BuildGraph(sie.Scope, sie.Solver.KnowledgeBase);
            foreach (var root in graph.GetRootNodes())
            {
                ProcessNode(root);
            }
            foreach (var inlined in InlineInContext(graph))
            {
                sie.Solver.KnowledgeBase.Retract(inlined.Predicate);
                sie.Solver.KnowledgeBase.AssertZ(inlined.InlinedPredicate);
                inlined.Predicate = inlined.InlinedPredicate;
            }
        }
        void ProcessNode(CompilerNode node, HashSet<Signature> visited = null)
        {
            visited ??= new HashSet<Signature>();
            node.InlinedPredicate = node.Predicate;
            if (visited.Contains(node.Signature))
            {
                node.IsCyclical = true;
                return; // Mark as cyclical and return. Don't throw error here.
            }

            visited.Add(node.Signature);

            foreach (var child in node.Dependents.Cast<CompilerNode>())
            {
                ProcessNode(child, visited);
            }

            // Only inline if not cyclical and if marked for inlining.
            if (!node.IsCyclical && InlinedPredicates.Contains(node.Signature))
            {
                node.IsInlined = true;
            }
        }
    }

    public IEnumerable<CompilerNode> InlineInContext(DependencyGraph<CompilerNode> graph)
    {
        foreach (var node in graph.GetRootNodes()
            .SelectMany(r => InlineNodeWithContext(r)))
        {
            yield return node;
        }
    }

    IEnumerable<CompilerNode> InlineNodeWithContext(CompilerNode node, HashSet<Signature> processed = null)
    {
        processed ??= new HashSet<Signature>();
        processed.Add(node.Signature);

        foreach (var dependent in node.Dependents.Cast<CompilerNode>())
        {
            if (node.IsInlined)
            {
                var newBody = Predicate.ExpandGoals(dependent.Predicate.Body, g =>
                {
                    if (g.Unify(node.Predicate.Head).TryGetValue(out var subs))
                    {
                        return node.Predicate.Body.Substitute(subs);
                    }
                    return g;
                });
                dependent.InlinedPredicate = dependent.InlinedPredicate.WithBody(newBody);
                yield return dependent;
                if (!processed.Contains(dependent.Signature))
                {
                    InlineNodeWithContext(dependent, processed);
                }
            }
        }
    }
}