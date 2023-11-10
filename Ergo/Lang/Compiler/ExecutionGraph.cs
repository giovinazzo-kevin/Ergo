using Ergo.Interpreter;

namespace Ergo.Lang.Compiler;

public class ExecutionGraph
{
    private Maybe<ErgoVM.Op> Compiled;
    public readonly ExecutionNode Root;

    public ExecutionGraph(ExecutionNode root)
    {
        if (root is SequenceNode seq)
            root = seq.AsRoot(); // Enables some optimizations
        Root = root;
    }
    private ErgoVM.Op CompileAndCache()
    {
        var op = Root.Compile();
        // var expl = Root.Explain();
        // Trace.WriteLine(expl);
        Compiled = op;
        return op;
    }

    public ExecutionGraph Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new(Root.Instantiate(ctx, vars));
    }
    public ExecutionGraph Substitute(IEnumerable<Substitution> s)
    {
        return new(Root.Substitute(s));
    }

    public ExecutionGraph Optimized() => new(new SequenceNode(new() { Root }).Optimize());

    /// <summary>
    /// Compiles the current graph to an Op that can run on the ErgoVM.
    /// Caches the result, since ExecutionGraphs are immutable by design and stored by Predicates.
    /// </summary>
    public ErgoVM.Op Compile()
    {
        return Compiled.GetOr(CompileAndCache());
    }
}

public static class ExecutionGraphExtensions
{
    private static readonly InstantiationContext CompilerContext = new("E");

    public static ExecutionGraph ToExecutionGraph(this Predicate clause, DependencyGraph graph, Dictionary<Signature, CyclicalCallNode> cyclicalCallMap = null)
    {
        var scope = graph.Scope/*.WithCurrentModule(clause.DeclaringModule)*/;
        var root = ToExecutionNode(clause.Body, graph, scope, cyclicalCallMap: cyclicalCallMap);
        return new(root);
    }

    public static ExecutionNode ToExecutionNode(this ITerm goal, DependencyGraph graph, Maybe<InterpreterScope> mbScope = default, InstantiationContext ctx = null, Dictionary<Signature, CyclicalCallNode> cyclicalCallMap = null)
    {
        ctx ??= CompilerContext;
        cyclicalCallMap ??= new();
        var scope = mbScope.GetOr(graph.Scope);// Handle the cyclical call node if it's present
        if (goal is NTuple tup)
        {
            var list = tup.Contents.Select(x => ToExecutionNode(x, graph, scope, ctx, cyclicalCallMap)).ToList();
            if (list.Count == 0)
                return TrueNode.Instance;
            if (list.Count == 1)
                return list[0];
            return new SequenceNode(list);
        }
        if (goal is Variable v)
            return new VariableNode(v);
        if (goal is Atom { Value: true })
            return TrueNode.Instance;
        if (goal is Atom { Value: false })
            return FalseNode.Instance;
        if (goal.Equals(WellKnown.Literals.Cut))
            return new CutNode();
        if (goal is Complex { Functor: var functor, Arity: var arity, Arguments: var args })
        {
            if (arity == 2 && WellKnown.Operators.If.Synonyms.Contains(functor))
            {
                return new IfThenNode(ToExecutionNode(args[0], graph, scope, ctx, cyclicalCallMap), ToExecutionNode(args[1], graph, scope, ctx, cyclicalCallMap));
            }
            if (arity == 2 && WellKnown.Operators.Disjunction.Synonyms.Contains(functor))
            {
                if (args[0] is Complex { Functor: var functor1, Arity: var arity1, Arguments: var args1 } && arity1 == 2 && WellKnown.Operators.If.Synonyms.Contains(functor1))
                {
                    return new IfThenElseNode(ToExecutionNode(args1[0], graph, scope, ctx, cyclicalCallMap), ToExecutionNode(args1[1], graph, scope, ctx, cyclicalCallMap), ToExecutionNode(args[1], graph, scope, ctx, cyclicalCallMap));
                }
                return new BranchNode(ToExecutionNode(args[0], graph, scope, ctx, cyclicalCallMap), ToExecutionNode(args[1], graph, scope, ctx, cyclicalCallMap));
            }
        }
        // If 'goal' isn't any other type of node, then it's a proper goal and we need to resolve it in the context of 'clause'.
        var matches = new List<ExecutionNode>();
        var sig = goal.GetSignature();
        // Qualified match
        if (graph.GetNode(sig).TryGetValue(out var node))
            Node(goal);
        // Qualified variadic match
        if (graph.GetNode(sig.WithArity(default)).TryGetValue(out node))
            matches.Add(new BuiltInNode(node, goal, node.Clauses.Single().BuiltIn.GetOrThrow(new InvalidOperationException())));
        // Resolve all possible callees
        if (!sig.Module.TryGetValue(out var module))
        {
            foreach (var possibleQualif in scope.VisibleModules)
            {
                sig = sig.WithModule(possibleQualif);
                // Match
                if (graph.GetNode(sig).TryGetValue(out node))
                    Node(goal.Qualified(possibleQualif));
                // Variadic match
                if (graph.GetNode(sig.WithArity(default)).TryGetValue(out node))
                    matches.Add(new BuiltInNode(node, goal, node.Clauses.Single().BuiltIn.GetOrThrow(new InvalidOperationException())));
                // Dynamic match
                if (scope.Modules[possibleQualif].DynamicPredicates.Contains(sig))
                    matches.Add(new DynamicNode(goal.Qualified(possibleQualif)));
            }
        }
        else
        {
            // Qualified dynamic match
            if (scope.Modules[module].DynamicPredicates.Contains(sig))
                matches.Add(new DynamicNode(goal));
        }
        if (matches.Count == 0)
            throw new CompilerException(ErgoCompiler.ErrorType.UnresolvedPredicate, goal.GetSignature().Explain());
        if (matches.Count == 1)
            return matches[0];
        return matches.Aggregate((a, b) => new BranchNode(a, b));

        void Node(ITerm goal)
        {
            //if (cyclicalCallMap.TryGetValue(sig, out var cyclical))
            //{
            //    matches.Add(new CyclicalCallNode(goal));
            //    return;
            //}
            if ((node.IsCyclical || node.Clauses.Any(c => c.IsTailRecursive)))
            {
                matches.Add(cyclicalCallMap[sig] = new CyclicalCallNode(goal));
                return;
            }
            foreach (var clause in node.Clauses)
            {
                if (Clause(clause).TryGetValue(out var match))
                    matches.Add(match);
            }
        }
        Maybe<ExecutionNode> Clause(Predicate clause)
        {
            var facts = new List<Predicate>();
            goal.GetQualification(out var head);
            if (clause.BuiltIn.TryGetValue(out var builtIn))
                return new BuiltInNode(node, head, builtIn);
            var instVars = new Dictionary<string, Variable>();
            var substitutedClause = clause.Instantiate(ctx, instVars);
            substitutedClause.Head.GetQualification(out var clauseHead);
            if (!head.Unify(clauseHead).TryGetValue(out var subs))
                return default;
            substitutedClause = Predicate.Substitute(substitutedClause, subs);
            substitutedClause.Head.GetQualification(out clauseHead);
            var unif = new Complex(WellKnown.Signatures.Unify.Functor, head, clauseHead);
            var unifDep = graph.GetNode(WellKnown.Signatures.Unify).GetOrThrow(new InvalidOperationException());
            var unifNode = new BuiltInNode(unifDep, unif, graph.UnifyInstance);
            if (clause.IsFactual)
            {
                return unifNode;
            }
            var execGraph = substitutedClause.ExecutionGraph
                .GetOr(ToExecutionGraph(substitutedClause, graph, cyclicalCallMap));
            if (!head.Equals(clauseHead))
            {
                return new SequenceNode(new() { unifNode, execGraph.Root });
            }
            return execGraph.Root;
        }
    }
}