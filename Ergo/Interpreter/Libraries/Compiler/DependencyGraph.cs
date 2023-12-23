﻿using Ergo.Interpreter;
using Ergo.Runtime.BuiltIns;

public class DependencyGraphNode
{
    public DependencyGraph Graph { get; set; }
    public List<Predicate> Clauses { get; } = new();
    public Signature Signature { get; set; }
    public HashSet<Signature> CalledSignatures { get; set; } = new();
    public HashSet<DependencyGraphNode> Dependencies { get; } = new();
    public HashSet<DependencyGraphNode> Dependents { get; } = new();
    public bool IsInlined { get; set; }
    public bool IsCyclical { get; set; }
    public List<Predicate> InlinedClauses { get; set; } = null;
}

public class DependencyGraph(KnowledgeBase knowledgeBase)
{
    private Dictionary<Signature, DependencyGraphNode> _nodes = new Dictionary<Signature, DependencyGraphNode>();
    public readonly KnowledgeBase KnowledgeBase = knowledgeBase;
    /// <summary>
    /// An instance of the Unify built-in that's scoped to this graph, enabling memoization.
    /// </summary>
    public readonly Unify UnifyInstance = new();

    // Populate nodes and dependencies from the solver's knowledge base and scoped built-ins

    public Signature GetKey(Predicate pred)
    {
        var sig = pred.Qualified().Head.GetSignature();
        if (pred.IsVariadic)
            sig = sig.WithArity(default);
        return sig;
    }

    public DependencyGraph Clone(KnowledgeBase newKb)
    {
        var dep = new DependencyGraph(newKb);
        dep._nodes = _nodes.ToDictionary();
        return dep;
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

    public void CalculateDependencies(Predicate pred)
    {
        var sig = GetKey(pred);
        var node = _nodes[sig];
        foreach (var calledSignature in node.CalledSignatures)
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

    public DependencyGraphNode AddNode(Predicate pred)
    {
        var sig = GetKey(pred);
        if (!_nodes.TryGetValue(sig, out var node))
        {
            node = new DependencyGraphNode { Signature = sig, Graph = this };
            _nodes[sig] = node;
        }
        node.Clauses.Add(pred);
        node.CalledSignatures.UnionWith(ExtractCalledSignatures(pred, KnowledgeBase));
        return node;
    }

    public DependencyGraphNode SetNode(Predicate pred)
    {
        var sig = GetKey(pred);
        var node = _nodes[sig] = new DependencyGraphNode() { Signature = sig };
        node.Clauses.Add(pred);
        return node;
    }

    public static IEnumerable<Signature> ExtractCalledSignatures(Predicate pred, KnowledgeBase kb)
    {
        var scope = kb.Scope.WithCurrentModule(pred.DeclaringModule);
        return Predicate.GetGoals(pred)
            .SelectMany(c => ExtractCalledSignatures(c, scope, kb));
    }

    public static IEnumerable<Signature> ExtractCalledSignatures(ITerm item, InterpreterScope scope, KnowledgeBase kb)
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
                var variant = signature.Functor.BuildAnonymousTerm(signature.Arity.GetOr(0));
                if (kb.Get(variant, out _).Any())
                {
                    yield return qualified;
                }
            }
        }
    }

    public Maybe<DependencyGraphNode> GetNode(Signature s) => _nodes.TryGetValue(s, out var node) ? node : Maybe<DependencyGraphNode>.None;

    public IEnumerable<DependencyGraphNode> GetRootNodes()
    {
        // Step 5: Identify Roots for Analysis
        return _nodes.Values.Where(node => node.Dependencies.Count == 0);
    }
    public IEnumerable<DependencyGraphNode> GetLeafNodes()
    {
        return _nodes.Values.Where(node => node.Dependents.Count == 0);
    }
    public IEnumerable<DependencyGraphNode> GetAllNodes()
    {
        return _nodes.Values;
    }
}
