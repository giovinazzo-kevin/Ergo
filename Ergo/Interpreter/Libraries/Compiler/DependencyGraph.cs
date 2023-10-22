using Ergo.Interpreter;
using Ergo.Solver.BuiltIns;

public class DependencyGraphNode
{
    public List<Predicate> Clauses { get; } = new();
    public Signature Signature { get; set; }
    public Maybe<SolverBuiltIn> BuiltIn { get; set; }
    public List<DependencyGraphNode> Dependencies { get; } = new List<DependencyGraphNode>();
    public List<DependencyGraphNode> Dependents { get; } = new List<DependencyGraphNode>();
    public bool IsInlined { get; set; }
    public bool IsCyclical { get; set; }
    public List<Predicate> InlinedClauses { get; set; } = null;
}

public class DependencyGraph
{
    private readonly Dictionary<Signature, DependencyGraphNode> _nodes = new Dictionary<Signature, DependencyGraphNode>();
    public readonly KnowledgeBase KnowledgeBase;
    public readonly InterpreterScope Scope;

    public DependencyGraph(InterpreterScope scope, KnowledgeBase knowledgeBase)
    {
        KnowledgeBase = knowledgeBase;
        Scope = scope;
        BuildGraph();
    }

    // Populate nodes and dependencies from the solver's knowledge base and scoped built-ins
    void BuildGraph()
    {
        foreach (var pred in KnowledgeBase)
        {
            var sig = pred.Qualified().Head.GetSignature();
            if (!_nodes.TryGetValue(sig, out var node))
            {
                node = new DependencyGraphNode { Signature = sig };
                _nodes[sig] = node;
            }
            node.Clauses.Add(pred);
        }

        foreach (var pred in KnowledgeBase)
        {
            var sig = pred.Qualified().Head.GetSignature();
            var node = _nodes[sig];
            foreach (var calledSignature in ExtractCalledPredicates(Scope.WithCurrentModule(pred.DeclaringModule), KnowledgeBase, pred))
            {
                if (_nodes.TryGetValue(calledSignature, out var calledNode))
                {
                    if (!node.Dependents.Contains(calledNode))
                        node.Dependencies.Add(calledNode);
                    if (!calledNode.Dependents.Contains(node))
                        calledNode.Dependents.Add(node);
                }
            }
        }
    }
    private IEnumerable<Signature> ExtractCalledPredicates(InterpreterScope scope, KnowledgeBase kb, Predicate pred)
    {
        return ExtractCalledSignaturesFromContents(scope, kb, pred.Body);
    }

    private IEnumerable<Signature> ExtractCalledSignaturesFromContents(InterpreterScope scope, KnowledgeBase kb, NTuple goals)
    {
        var allGoals = new List<ITerm>();
        _ = Predicate.ExpandGoals(goals, t => { allGoals.Add(t); return t; });
        foreach (var item in allGoals)
        {
            var signature = item.GetSignature();
            if (signature.Module.TryGetValue(out _))
                yield return signature;
            else
            {
                // Unqualified signature. What does it resolve to?
                // A conjunction of clauses, possibly from different modules. Which ones?
                // Let's just try resolving them.
                foreach (var m in scope.VisibleModules)
                {
                    var qualified = signature.WithModule(m);
                    if (kb.Get(qualified).TryGetValue(out _))
                    {
                        yield return qualified;
                    }
                }
            }
        }
    }

    public Maybe<DependencyGraphNode> GetNode(Signature s) => _nodes.TryGetValue(s, out var node) ? node : default;

    public IEnumerable<DependencyGraphNode> GetRootNodes()
    {
        // Step 5: Identify Roots for Analysis
        return _nodes.Values.Where(node => !node.Dependencies.Any());
    }
    public IEnumerable<DependencyGraphNode> GetLeafNodes()
    {
        return _nodes.Values.Where(node => !node.Dependents.Any());
    }
    public IEnumerable<DependencyGraphNode> GetAllNodes()
    {
        return _nodes.Values;
    }
}
