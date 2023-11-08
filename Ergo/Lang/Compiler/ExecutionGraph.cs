using Ergo.Interpreter;
using Ergo.Solver;
#if ERGO_COMPILER_DIAGNOSTICS
using System.Diagnostics;
#endif

namespace Ergo.Lang.Compiler;

public readonly struct ExecutionGraph
{
    public readonly ExecutionNode Root;

    public ExecutionGraph(ExecutionNode root) => Root = root;

    public ExecutionGraph Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new(Root.Instantiate(ctx, vars));
    }
    public ExecutionGraph Substitute(IEnumerable<Substitution> s)
    {
        return new(Root.Substitute(s));
    }

    public ExecutionGraph Optimized() => new(Root.Optimize());

    public IEnumerable<Solution> Execute(SolverContext ctx, SolverScope scope)
    {
        var vm = new ErgoVM() { Context = ctx, Scope = scope, KnowledgeBase = ctx.Solver.KnowledgeBase };
        vm.Query = Root.Compile();
        return vm.RunInteractive();
    }
}

public static class ExecutionGraphExtensions
{
    private static readonly InstantiationContext CompilerContext = new("__E");

    public static ExecutionGraph ToExecutionGraph(this Predicate clause, DependencyGraph graph)
    {
        var scope = graph.Scope/*.WithCurrentModule(clause.DeclaringModule)*/;
        var root = ToExecutionNode(clause.Body, graph, scope);
        if (root is SequenceNode seq)
            root = seq.AsRoot(); // Enables some optimizations
        return new(root);
    }

    public static ExecutionNode ToExecutionNode(this ITerm goal, DependencyGraph graph, Maybe<InterpreterScope> mbScope = default, InstantiationContext ctx = null)
    {
        ctx ??= CompilerContext;
        var scope = mbScope.GetOr(graph.Scope);
        if (goal is NTuple tup)
        {
            var list = tup.Contents.Select(x => ToExecutionNode(x, graph, scope, ctx)).ToList();
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
                return new IfThenNode(ToExecutionNode(args[0], graph, scope, ctx), ToExecutionNode(args[1], graph, scope, ctx));
            }
            if (arity == 2 && WellKnown.Operators.Disjunction.Synonyms.Contains(functor))
            {
                if (args[0] is Complex { Functor: var functor1, Arity: var arity1, Arguments: var args1 } && arity1 == 2 && WellKnown.Operators.If.Synonyms.Contains(functor1))
                {
                    return new IfThenElseNode(ToExecutionNode(args1[0], graph, scope, ctx), ToExecutionNode(args1[1], graph, scope, ctx), ToExecutionNode(args[1], graph, scope, ctx));
                }
                return new BranchNode(ToExecutionNode(args[0], graph, scope, ctx), ToExecutionNode(args[1], graph, scope, ctx));
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
            var facts = new List<Predicate>();
            goal.GetQualification(out var head);
            if (!node.IsCyclical)
            {
                foreach (var clause in node.Clauses)
                {
                    if (clause.BuiltIn.TryGetValue(out var builtIn))
                    {
                        matches.Add(new BuiltInNode(node, head, builtIn));
                        continue;
                    }
                    var substitutedClause = clause.Instantiate(ctx);
                    substitutedClause.Head.GetQualification(out var clauseHead);
                    if (head.Unify(clauseHead).TryGetValue(out var subs))
                    {
                        substitutedClause = Predicate.Substitute(substitutedClause, subs);
                    }
                    else continue;
                    substitutedClause.Head.GetQualification(out clauseHead);
                    var unif = new Complex(WellKnown.Signatures.Unify.Functor, head, clauseHead);
                    var unifDep = graph.GetNode(WellKnown.Signatures.Unify).GetOrThrow(new InvalidOperationException());
                    var unifNode = new BuiltInNode(unifDep, unif, graph.UnifyInstance);
                    if (clause.IsFactual)
                    {
                        matches.Add(unifNode);
                        continue;
                    }
                    var inner = ToExecutionGraph(substitutedClause, graph).Root;
                    if (!head.Equals(clauseHead))
                    {
                        var seq = new SequenceNode(new() { unifNode, inner });
                        matches.Add(seq);
                    }
                    else
                    {
                        matches.Add(inner);
                    }
                }
            }
            else
            {
                matches.Add(new CyclicalGoalNode(node, goal));
            }
        }
    }
}