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
        public List<Predicate> InlinedClauses { get; set; } = null;

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
            if (!sie.Solver.Flags.HasFlag(Solver.SolverFlags.EnableInlining))
                return;
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
                foreach (var (pred, clause) in inlined.Clauses.Zip(inlined.InlinedClauses))
                {
                    sie.Solver.KnowledgeBase.Retract(pred);
                    sie.Solver.KnowledgeBase.AssertZ(clause);
                }
                inlined.Clauses.Clear();
                inlined.Clauses.AddRange(inlined.InlinedClauses);
            }
        }
        void ProcessNode(CompilerNode node, HashSet<Signature> visited = null)
        {
            visited ??= new HashSet<Signature>();
            if (visited.Contains(node.Signature))
            {
                node.IsCyclical = true;
                return; // Mark as cyclical and return. Don't throw error here.
            }
            node.InlinedClauses ??= new(node.Clauses);
            visited.Add(node.Signature);
            foreach (var child in node.Dependents.Cast<CompilerNode>())
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

    public IEnumerable<CompilerNode> InlineInContext(DependencyGraph<CompilerNode> graph)
    {
        var ctx = new InstantiationContext("__I");
        foreach (var node in graph.GetRootNodes()
            .SelectMany(r => InlineNodeWithContext(ctx, r)))
        {
            yield return node;
        }
    }

    IEnumerable<CompilerNode> InlineNodeWithContext(InstantiationContext ctx, CompilerNode node, HashSet<Signature> processed = null)
    {
        processed ??= new HashSet<Signature>();
        processed.Add(node.Signature);
        foreach (var dependent in node.Dependents.Cast<CompilerNode>())
        {
            if (node.IsInlined)
            {
                if (node.InlinedClauses.Count == 1)
                {
                    var inlined = node.InlinedClauses.Single().Instantiate(ctx);
                    foreach (var ret in Inline(inlined, dependent))
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
                    foreach (var ret in Inline(inlined, dependent))
                        yield return ret;
                }
            }
            if (!processed.Contains(dependent.Signature))
            {
                foreach (var inner in InlineNodeWithContext(ctx, dependent, processed))
                    yield return inner;
            }
        }

        IEnumerable<CompilerNode> Inline(Predicate inlined, CompilerNode dependent)
        {
            var newClauses = new List<Predicate>();
            foreach (var clause in dependent.InlinedClauses)
            {
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
    public static List<List<Predicate>> GroupByVariantHeads(List<Predicate> clauses)
    {
        var hashTable = new Dictionary<ITerm, List<Predicate>>();

        foreach (var clause in clauses)
        {
            bool foundGroup = false;

            var inst = clause.Instantiate(new("_TMP"));
            foreach (var key in hashTable.Keys)
            {
                if (clause.Head.IsVariantOf(key))
                {
                    hashTable[key].Add(inst);
                    foundGroup = true;
                    break;
                }
            }

            if (!foundGroup)
            {
                var newGroup = new List<Predicate> { inst };
                hashTable[clause.Head] = newGroup;
            }
        }
        return hashTable.Values.ToList();
    }
}