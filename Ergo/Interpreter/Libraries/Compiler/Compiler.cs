using Ergo.Events;
using Ergo.Events.Solver;
using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.Compiler;

using Ergo.Lang.Compiler;
using Ergo.Solver;
using System.Collections.Generic;

public class Compiler : Library
{
    public override int LoadOrder => 100;

    public DependencyGraph DependencyGraph { get; private set; }

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
            DependencyGraph = new DependencyGraph(sie.Scope, sie.Solver.KnowledgeBase);
            // The concept is similar to term expansions, but instead of recursively expanding each term,
            // inlining works at the goal level, allowing it to be qualified with a module and thus be more specific.
            foreach (var root in DependencyGraph.GetRootNodes())
            {
                ProcessNode(root);
            }
            if (sie.Solver.Flags.HasFlag(SolverFlags.EnableInliner))
            {
                foreach (var inlined in InlineInContext(sie.Scope, DependencyGraph))
                {
                    foreach (var (pred, clause) in inlined.Clauses.Zip(inlined.InlinedClauses))
                    {
                        sie.Solver.KnowledgeBase.Retract(pred);
                        sie.Solver.KnowledgeBase.AssertZ(clause);
                    }
                    inlined.Clauses.Clear();
                    inlined.Clauses.AddRange(inlined.InlinedClauses);
                }
            }
            //if (sie.Solver.Flags.HasFlag(SolverFlags.EnableCompiler))
            //{
            //    // Now that everything has been inlined, we can build the execution graph for each predicate.
            //    foreach (var node in DependencyGraph.GetAllNodes())
            //    {
            //        for (int i = node.Clauses.Count - 1; i >= 0; --i)
            //        {
            //            var pred = node.Clauses[i];
            //            if (!pred.IsBuiltIn)
            //            {
            //                if (!sie.Scope.ExceptionHandler.TryGet(() => pred.ToExecutionGraph(DependencyGraph)).TryGetValue(out var execGraph))
            //                    continue;
            //                sie.Solver.KnowledgeBase.Retract(pred);
            //                pred = pred.WithExecutionGraph(execGraph);
            //                sie.Solver.KnowledgeBase.AssertZ(pred);
            //                node.Clauses.RemoveAt(i);
            //                node.Clauses.Insert(i, pred);
            //            }
            //        }
            //    }
            //}
        }
        else if (evt is QuerySubmittedEvent qse)
        {
            var topLevelHead = new Complex(WellKnown.Literals.TopLevel, qse.Query.Goals.Contents.SelectMany(g => g.Variables).Distinct().Cast<ITerm>().ToArray());
            foreach (var match in qse.Solver.KnowledgeBase.GetMatches(qse.Scope.InstantiationContext, topLevelHead, desugar: false)
                .AsEnumerable().SelectMany(x => x))
            {
                var topLevel = match.Predicate;
                if (qse.Solver.Flags.HasFlag(SolverFlags.EnableCompiler))
                {
                    qse.Solver.KnowledgeBase.Retract(topLevel);
                    if (!qse.Scope.InterpreterScope.ExceptionHandler.TryGet(() => topLevel.WithExecutionGraph(topLevel.ToExecutionGraph(DependencyGraph)))
                        .TryGetValue(out topLevel))
                        return;
                    qse.Solver.KnowledgeBase.AssertZ(topLevel);
                }
            }
        }

        void ProcessNode(DependencyGraphNode node, HashSet<Signature> visited = null)
        {
            visited ??= new HashSet<Signature>();
            if (visited.Contains(node.Signature))
            {
                node.IsCyclical = true;
                return; // Mark as cyclical and return. Don't throw error here.
            }
            node.InlinedClauses ??= new(node.Clauses);
            visited.Add(node.Signature);
            foreach (var child in node.Dependents.Cast<DependencyGraphNode>())
            {
                var copy = visited.ToHashSet();
                ProcessNode(child, copy);
            }
            // Only inline if not cyclical and if marked for inlining.
            if (!node.IsInlined && !node.IsCyclical && InlinedPredicates.Contains(node.Signature))
            {
                node.IsInlined = true;
            }
        }
    }

    public IEnumerable<DependencyGraphNode> InlineInContext(InterpreterScope scope, DependencyGraph graph)
    {
        var ctx = new InstantiationContext("__I");
        foreach (var node in graph.GetRootNodes()
            .SelectMany(r => InlineNodeWithContext(scope, ctx, r)))
        {
            yield return node;
        }
    }

    IEnumerable<DependencyGraphNode> InlineNodeWithContext(InterpreterScope scope, InstantiationContext ctx, DependencyGraphNode node, DependencyGraphNode dependent, HashSet<Signature> processed = null)
    {
        processed ??= new HashSet<Signature>();
        processed.Add(node.Signature);
        if (node.IsInlined)
        {
            // At this point we should take a step back and see if other nodes that 'dependent' depends on may be confused for this one.
            // In that case they should either be merged, being mindful of which ones should be inlined and which ones shouldn't, or they should not be inlined at all.
            // An example comes when overloading an operator, such as =/2 or :=/2. In particular =/2 is overloaded by the dict module.
            // The result is that when the dict module's definition matches, the default implementation is ignored, cut away.
            // These semantics should be preserved when inlining =/2 and similar predicates. A naive approach might elide other definitions.
            // A smart approach will inline them individually and merge them as a disjunction. A pragmatic approach will just avoid this head scratcher.
            var lookup = dependent.Dependencies
                .Except(new[] { node })
                .ToLookup(x => x.Signature.WithModule(default));
            var sig = node.Signature.WithModule(default);
            if (lookup.Contains(sig))
            {
                var clashes = lookup[sig];
                // If we're inlining prologue:=/2, then clashes will contain dict:=/2.
                // If we can unambiguously tell which definition is being called by dependent, we can inline that definition only.
                // Otherwise we'll take the pragmatic path since inlining disjunctions is as efficient as regular matching.
                yield break;
            }
            else
            {
                foreach (var ret in Inline(node, dependent))
                    yield return ret;
            }
        }
        if (!processed.Contains(dependent.Signature))
        {
            foreach (var inner in InlineNodeWithContext(scope, ctx, dependent, processed))
                yield return inner;
        }


        IEnumerable<DependencyGraphNode> Inline(DependencyGraphNode node, DependencyGraphNode dependent)
        {
            if (node.InlinedClauses.Count == 1)
            {
                var inlined = node.InlinedClauses.Single().Instantiate(ctx);
                foreach (var ret in InlineInner(inlined, dependent))
                    yield return ret;
            }
            else
            {
                // Inlining predicates with multiple clauses is a bit hairier.
                // The bulk of the work is done by Predicate.InlineHead, which turns all arguments into variables
                // and moves their unification to a precondition in the body. This normalizes all variants to the same form.
                var normalized = node.InlinedClauses.Select(x => Predicate.InlineHead(new("_TMP"), x)).ToList();
                var newBody = normalized
                    .Select(g => (ITerm)g.Body)
                     .Aggregate((a, b) => WellKnown.Operators.Disjunction.ToComplex(a, Maybe.Some(b)));
                // We can pick any one normalized head to act as the "most general"
                var inlined = normalized[0].WithBody(new NTuple(new[] { newBody }));
                foreach (var ret in InlineInner(inlined, dependent))
                    yield return ret;
            }
        }

        IEnumerable<DependencyGraphNode> InlineInner(Predicate inlined, DependencyGraphNode dependent)
        {
            var newClauses = new List<Predicate>();
            foreach (var clause in dependent.InlinedClauses)
            {
                if (clause.IsBuiltIn)
                {
                    newClauses.Add(clause);
                    continue;
                }
                var newBody = Predicate.ExpandGoals(clause.Body, g =>
                {
                    if (g.Unify(inlined.Head).TryGetValue(out var subs))
                        return inlined.Body.Substitute(subs);
                    return g;
                });
                // ExpandGoals already removes empty statements (true), but at the end it may return an empty list.
                if (newBody.Contents.Length == 1 && newBody.Contents.Single().Equals(WellKnown.Literals.EmptyCommaList))
                    // In that case the predicate simply turns into a fact.
                    newBody = new NTuple(new ITerm[] { WellKnown.Literals.True });
                newClauses.Add(clause.WithBody(newBody));
            }
            dependent.InlinedClauses.Clear();
            dependent.InlinedClauses.AddRange(newClauses);
            yield return dependent;
        }
    }

    IEnumerable<DependencyGraphNode> InlineNodeWithContext(InterpreterScope scope, InstantiationContext ctx, DependencyGraphNode node, HashSet<Signature> processed = null)
    {
        processed ??= new HashSet<Signature>();
        foreach (var inline in node.Dependents.Cast<DependencyGraphNode>()
            .SelectMany(d => InlineNodeWithContext(scope, ctx, node, d, processed)))
        {
            yield return inline;
        }
    }
}