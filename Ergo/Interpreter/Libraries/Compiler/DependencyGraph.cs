using Ergo.Interpreter;

public class DependencyGraphNode
{
    public Predicate Predicate { get; set; }
    public Predicate QualifiedPredicate { get; set; }
    public Signature Signature { get; set; }
    public List<DependencyGraphNode> Dependencies { get; } = new List<DependencyGraphNode>();
    public List<DependencyGraphNode> Dependents { get; } = new List<DependencyGraphNode>();
}

public class DependencyGraph<T>
    where T : DependencyGraphNode, new()
{
    private readonly Dictionary<Signature, T> _nodes = new Dictionary<Signature, T>();

    // Populate nodes and dependencies from the solver's knowledge base
    public void BuildGraph(InterpreterScope scope, KnowledgeBase knowledgeBase)
    {
        // Step 2: Populate the Nodes
        foreach (var pred in knowledgeBase)
        {
            var node = new T
            {
                QualifiedPredicate = pred.Qualified(),
                Predicate = pred
            };
            node.Signature = node.QualifiedPredicate.Head.GetSignature();
            _nodes[node.Signature] = node;
        }

        // Step 3: Populate the Edges
        foreach (var pred in knowledgeBase)
        {
            var node = _nodes[pred.Qualified().Head.GetSignature()];
            foreach (var calledSignature in ExtractCalledPredicates(scope.WithCurrentModule(pred.DeclaringModule), knowledgeBase, pred))
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
        return ExtractCalledSignaturesFromContents(scope, kb, pred.Body.Contents);
    }

    private IEnumerable<Signature> ExtractCalledSignaturesFromContents(InterpreterScope scope, KnowledgeBase kb, IEnumerable<ITerm> contents)
    {
        foreach (var item in contents)
        {
            if (item is NTuple innerList)
            {
                foreach (var innerSig in ExtractCalledSignaturesFromContents(scope, kb, innerList.Contents))
                {
                    yield return innerSig;
                }
                continue;
            }
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

    public Maybe<T> GetNode(Signature s) => _nodes.TryGetValue(s, out var node) ? node : default;

    public IEnumerable<T> GetRootNodes()
    {
        // Step 5: Identify Roots for Analysis
        return _nodes.Values.Where(node => !node.Dependencies.Any());
    }
    public IEnumerable<T> GetLeafNodes()
    {
        return _nodes.Values.Where(node => !node.Dependents.Any());
    }
    public IEnumerable<T> GetAllNodes()
    {
        return _nodes.Values;
    }
}
