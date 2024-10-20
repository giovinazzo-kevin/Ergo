using Ergo.Modules;
using Ergo.Runtime.BuiltIns;

public class LegacyDependencyGraphNode
{
    public LegacyDependencyGraph Graph { get; set; }
    public List<Clause> Clauses { get; } = [];
    public Signature Signature { get; set; }
    public HashSet<LegacyDependencyGraphNode> Dependencies { get; } = [];
    public HashSet<LegacyDependencyGraphNode> Dependents { get; } = [];
    public bool IsInlined { get; set; }
    public bool IsCyclical { get; set; }
    public List<Clause> InlinedClauses { get; set; } = null;
}

public class LegacyDependencyGraph
{
    private readonly Dictionary<Signature, LegacyDependencyGraphNode> _nodes = [];
    public readonly ErgoKnowledgeBase KnowledgeBase;
    /// <summary>
    /// An instance of the Unify built-in that's scoped to this graph, enabling memoization.
    /// </summary>
    public readonly Unify UnifyInstance = new();

    public LegacyDependencyGraph(ErgoKnowledgeBase knowledgeBase)
    {
        KnowledgeBase = knowledgeBase;
    }

    // Populate nodes and dependencies from the solver's knowledge base and scoped built-ins

    public Signature GetKey(Clause pred)
    {
        var sig = pred.Qualified().Head.GetSignature();
        if (pred.IsVariadic)
            sig = sig.WithArity(default);
        return sig;
    }

    public void Rebuild()
    {
        _nodes.Clear();
        foreach (var pred in KnowledgeBase)
        {
            AddNode(pred);
        }
        foreach (var pred in KnowledgeBase)
        {
            CalculateDependencies(pred);
        }
    }

    protected bool IsCyclical(LegacyDependencyGraphNode node)
    {
        if (node.IsCyclical)
            return true;
        var visited = new HashSet<LegacyDependencyGraphNode>();
        return Inner(node, node, visited);

        bool Inner(LegacyDependencyGraphNode cycle, LegacyDependencyGraphNode node, HashSet<LegacyDependencyGraphNode> visited)
        {
            visited.Add(node);
            foreach (var dep in node.Dependencies)
            {
                if (dep == cycle)
                    return true;
                if (visited.Contains(dep))
                    return false;
                if (Inner(cycle, dep, visited))
                    return true;
            }
            return false;
        }
    }

    public void CalculateDependencies(Clause pred)
    {
        var sig = GetKey(pred);
        var node = _nodes[sig];
        foreach (var calledSignature in ExtractCalledSignatures(pred, KnowledgeBase))
        {
            if (_nodes.TryGetValue(calledSignature, out var calledNode))
            {
                if (!node.Dependents.Contains(calledNode))
                    node.Dependencies.Add(calledNode);
                if (!calledNode.Dependents.Contains(node))
                    calledNode.Dependents.Add(node);
                calledNode.IsCyclical = IsCyclical(calledNode);
            }
        }
    }

    public LegacyDependencyGraphNode AddNode(Clause pred)
    {
        var sig = GetKey(pred);
        if (!_nodes.TryGetValue(sig, out var node))
        {
            node = new LegacyDependencyGraphNode { Signature = sig, Graph = this };
            _nodes[sig] = node;
        }
        node.Clauses.Add(pred);
        return node;
    }

    public LegacyDependencyGraphNode SetNode(Clause pred)
    {
        var sig = GetKey(pred);
        var node = _nodes[sig] = new LegacyDependencyGraphNode() { Signature = sig };
        node.Clauses.Add(pred);
        return node;
    }

    public static IEnumerable<Signature> ExtractCalledSignatures(Clause pred, ErgoKnowledgeBase kb)
    {
        var scope = kb.Scope.WithCurrentModule(pred.DeclaringModule);
        return Clause.GetGoals(pred)
            .SelectMany(c => ExtractCalledSignatures(c, scope, kb));
    }

    public static IEnumerable<Signature> ExtractCalledSignatures(ITerm item, InterpreterScope scope, ErgoKnowledgeBase kb)
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

    public Maybe<LegacyDependencyGraphNode> GetNode(Signature s) => _nodes.TryGetValue(s, out var node) ? node : Maybe<LegacyDependencyGraphNode>.None;

    public IEnumerable<LegacyDependencyGraphNode> GetRootNodes()
    {
        // Step 5: Identify Roots for Analysis
        return _nodes.Values.Where(node => !node.Dependencies.Any());
    }
    public IEnumerable<LegacyDependencyGraphNode> GetLeafNodes()
    {
        return _nodes.Values.Where(node => !node.Dependents.Any());
    }
    public IEnumerable<LegacyDependencyGraphNode> GetAllNodes()
    {
        return _nodes.Values;
    }
}
