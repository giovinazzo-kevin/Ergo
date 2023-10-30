using Ergo.Solver;

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

    public IEnumerable<Solution> Execute(SolverContext ctx, SolverScope scope)
    {
        var execScope = ExecutionScope.Empty;
        foreach (var step in Root.Execute(ctx, scope, execScope))
        {
            if (!step.IsSolution)
                continue;
            yield return new(scope, step.CurrentSubstitutions);
        }
    }
}

public static class ExecutionGraphExtensions
{
    public static ExecutionGraph ToExecutionGraph(this Predicate clause, DependencyGraph graph)
    {
        var scope = graph.Scope.WithCurrentModule(clause.DeclaringModule);
        return new(ToExecutionNode(clause.Body));

        ExecutionNode ToExecutionNode(ITerm goal)
        {
            if (goal is NTuple tup)
            {
                var list = tup.Contents.Select(x => ToExecutionNode(x)).ToList();
                if (list.Count == 1)
                    return list[0];
                return new SequenceNode(list);
            }
            if (goal is Variable v)
                return new VariableNode(v);
            if (goal is Atom { Value: true })
                return new TrueNode();
            if (goal is Atom { Value: false })
                return new FalseNode();
            if (goal.Equals(WellKnown.Literals.Cut))
                return new CutNode();
            if (goal is Complex { Functor: var functor, Arity: var arity, Arguments: var args })
            {
                if (arity == 2 && WellKnown.Operators.If.Synonyms.Contains(functor))
                {
                    return new IfThenNode(ToExecutionNode(args[0]), ToExecutionNode(args[1]));
                }
                if (arity == 2 && WellKnown.Operators.Disjunction.Synonyms.Contains(functor))
                {
                    if (args[0] is Complex { Functor: var functor1, Arity: var arity1, Arguments: var args1 } && arity1 == 2 && WellKnown.Operators.If.Synonyms.Contains(functor1))
                    {
                        return new IfThenElseNode(ToExecutionNode(args1[0]), ToExecutionNode(args1[1]), ToExecutionNode(args[1]));
                    }
                    return new BranchNode(ToExecutionNode(args[0]), ToExecutionNode(args[1]));
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
            if (matches.Count == 0)
            {
                throw new CompilerException(ErgoCompiler.ErrorType.UnresolvedPredicate, goal.GetSignature().Explain());
            }
            if (matches.Count == 1)
                return matches[0];
            return matches.Aggregate((a, b) => new BranchNode(a, b));

            void Node(ITerm goal)
            {
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
                        if (clause.IsTailRecursive)
                        {

                        }
                        var substitutedClause = clause;
                        if (head.Unify(clause.Head).TryGetValue(out var subs))
                        {
                            substitutedClause = Predicate.Substitute(substitutedClause, subs);
                        }
                        matches.Add(ToExecutionGraph(substitutedClause, graph).Root);
                    }
                }
                else
                {
                    matches.Add(new CyclicalGoalNode(node, goal));
                }
            }
        }
    }
}