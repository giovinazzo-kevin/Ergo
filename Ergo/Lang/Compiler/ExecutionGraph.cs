﻿using Ergo.Modules;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Lang.Compiler;

public class ExecutionGraph
{
    private Maybe<Op> Compiled;
    public readonly ExecutionNode Root;
    public readonly ITerm Head;

    public ExecutionGraph(ITerm head, ExecutionNode root)
    {
        if (root is SequenceNode seq)
            root = seq.AsRoot(); // Enables some optimizations
        Root = root;
        head.GetQualification(out Head);
    }
    private Op CompileAndCache()
    {
        Root.Analyze(); // Do static analysis on the optimized graph before compiling
        if (Root is not SequenceNode)
            Root.IsContinuationDet = true;
        var compiledRoot = Root.Compile();
        //Debug.WriteLine(Root.Explain(false));
        // NOTE: PrepareDelegate pre-JITs 'op' so that we don't incur JIT overhead at runtime.
        RuntimeHelpers.PrepareDelegate(compiledRoot);
        Compiled = compiledRoot;
        return compiledRoot;
    }
    public ExecutionGraph Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (Root.IsGround)
            return this;
        vars ??= new();
        return new(Head.Instantiate(ctx, vars), Root.Instantiate(ctx, vars))
        { Compiled = Compiled };
    }
    public ExecutionGraph Substitute(IEnumerable<Substitution> s)
    {
        if (Root.IsGround)
            return this;
        return new(Head.Substitute(s), Root.Substitute(s))
        { Compiled = Compiled };
    }

    public ExecutionGraph Optimized() => new(Head, new SequenceNode(new() { Root }).Optimize());

    /// <summary>
    /// Compiles the current graph to a Goal that can run on the ErgoVM.
    /// Caches the result, since ExecutionGraphs are immutable by design and stored by Predicates.
    /// </summary>
    public Op Compile()
    {
        return Compiled.GetOr(CompileAndCache());
    }
}

public static class ExecutionGraphExtensions
{
    public static readonly InstantiationContext CompilerContext = new("E");

    public static ExecutionGraph ToExecutionGraph(this Clause clause, ErgoDependencyGraph graph, Dictionary<Signature, CyclicalCallNode> cyclicalCallMap = null)
    {
        var root = ToExecutionNode(clause.Body, graph, graph.KnowledgeBase.Scope, clause.DeclaringModule, cyclicalCallMap: cyclicalCallMap);
        return new(clause.Head, root);
    }

    public static ExecutionNode ToExecutionNode(this ITerm goal, ErgoDependencyGraph graph, Maybe<InterpreterScope> mbScope = default, Maybe<Atom> callerModule = default, InstantiationContext ctx = null, Dictionary<Signature, CyclicalCallNode> cyclicalCallMap = null)
    {
        ctx ??= CompilerContext;
        cyclicalCallMap ??= new();
        var ret = default(ExecutionNode);
        Action<ExecutionNode> onReturn = _ => { };
        var scope = mbScope.GetOr(graph.KnowledgeBase.Scope);// Handle the cyclical call node if it's present
        if (goal is NTuple tup)
        {
            var list = tup.Contents.Select(x => ToExecutionNode(x, graph, scope, callerModule, ctx, cyclicalCallMap)).ToList();
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
                return new IfThenNode(ToExecutionNode(args[0], graph, scope, callerModule, ctx, cyclicalCallMap), ToExecutionNode(args[1], graph, scope, callerModule, ctx, cyclicalCallMap));
            }
            if (arity == 2 && WellKnown.Operators.Disjunction.Synonyms.Contains(functor))
            {
                if (args[0] is Complex { Functor: var functor1, Arity: var arity1, Arguments: var args1 } && arity1 == 2 && WellKnown.Operators.If.Synonyms.Contains(functor1))
                {
                    return new IfThenElseNode(ToExecutionNode(args1[0], graph, scope, callerModule, ctx, cyclicalCallMap), ToExecutionNode(args1[1], graph, scope, callerModule, ctx, cyclicalCallMap), ToExecutionNode(args[1], graph, scope, callerModule, ctx, cyclicalCallMap));
                }
                return new BranchNode(ToExecutionNode(args[0], graph, scope, callerModule, ctx, cyclicalCallMap), ToExecutionNode(args[1], graph, scope, callerModule, ctx, cyclicalCallMap));
            }
        }
        // If 'goal' isn't any other type of node, then it's a proper goal and we need to resolve it in the context of 'clause'.
        var found = false;
        var matches = new List<ExecutionNode>();
        var sig = goal.GetSignature();
        // Qualified match
        if (graph.GetNode(sig).TryGetValue(out var node))
        {
            Node(goal);
            found = true;
        }
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
                {
                    Node(goal.Qualified(possibleQualif));
                    found = true;
                }
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
        if (matches.Count == 0 && !found)
            throw new CompilerException(ErgoCompiler.ErrorType.UnresolvedPredicate, goal.GetSignature().Explain());
        else if (matches.Count == 0 && found)
            return FalseNode.Instance;
        if (matches.Count == 1)
            ret = matches[0];
        else ret = matches.Aggregate((a, b) => new BranchNode(a, b));
        onReturn(ret);
        return ret;
        void Node(ITerm goal)
        {
            if (cyclicalCallMap.TryGetValue(sig, out var cyclical))
            {
                matches.Add(new CyclicalCallNode(goal) { Clause = cyclical.Clause, Ref = cyclical.Ref });
                return;
            }
            if (node.IsCyclical || node.Clauses.Any(c => c.IsTailRecursive))
            {
                cyclicalCallMap[sig] = new CyclicalCallNode(goal);
            }
            var newMatches = new List<ExecutionNode>();
            foreach (var clause in node.Clauses
                .OrderByDescending(x => scope.Modules.TryGetValue(x.DeclaringModule, out var m) ? m.LoadOrder : 0))
            {
                if (Clause(clause, callerModule.GetOr(scope.Entry)).TryGetValue(out var match))
                    newMatches.Add(match);
            }
            matches.AddRange(newMatches);
            if (cyclicalCallMap.TryGetValue(sig, out var c))
            {
                onReturn += ret =>
                c.Ref.Node = ret;
            }
        }
        Maybe<ExecutionNode> Clause(Clause clause, Atom callerModule)
        {
            var facts = new List<Clause>();
            goal.GetQualification(out var head);
            if (scope.Modules.TryGetValue(clause.DeclaringModule, out var module))
            {
                var sig = clause.Head.Qualified(clause.DeclaringModule).GetSignature();
                if (module.MetaPredicates.TryGetValue(sig, out var meta))
                {
                    var args = head.GetArguments();
                    for (int i = 0; i < meta.Arguments.Length; i++)
                    {
                        args = args.SetItem(i, meta.Arguments[i] switch
                        {
                            '+' when !args[i].GetQualification(out _).HasValue => args[i].Qualified(callerModule),
                            _ => args[i],
                        });
                    }
                    if (head is Complex cplxHead)
                        head = cplxHead.WithArguments(args);
                }
            }
            if (clause.BuiltIn.TryGetValue(out var builtIn))
                return new BuiltInNode(node, head, builtIn);
            var substitutedClause = clause.Instantiate(ctx);
            substitutedClause.Head.GetQualification(out var clauseHead);
            if (!head.Unify(clauseHead).TryGetValue(out var subs))
                return default;
            substitutedClause = substitutedClause.Substitute(subs);
            substitutedClause.Head.GetQualification(out clauseHead);
            var unif = Unify.MakeComplex(head, clauseHead);
            var unifDep = graph.GetNode(WellKnown.Signatures.Unify).GetOrThrow(new InvalidOperationException());
            var unifNode = new BuiltInNode(unifDep, unif, graph.UnifyInstance);
            if (clause.IsFactual)
                return unifNode;
            if (cyclicalCallMap.TryGetValue(sig, out var cycleNode))
                cycleNode.Clause = clause;
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