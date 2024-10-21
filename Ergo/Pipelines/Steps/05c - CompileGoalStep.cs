using Ergo.Compiler;

namespace Ergo;

public interface ICompileGoalStep : IErgoPipeline<GoalDefinition, CallNode, ICompileGoalStep.Env>
{
    public interface Env
    {
        InstantiationContext InstantiationContext { get; }
        ErgoDependencyGraph DependencyGraph { get; }
        ErgoExecutionGraph ExecutionGraph { get; }
    }
}

public class CompileGoalStep : ICompileGoalStep
{
    public Either<CallNode, PipelineError> Run(GoalDefinition def, ICompileGoalStep.Env env)
    {
        //if (!env.ExecutionGraph.Predicates.TryGetValue(def.Signature, out var pred))
        //    throw new InvalidOperationException();
        //var nodes = new List<GoalNode>();
        //if (pred.BuiltIn.TryGetValue(out var builtIn))
        //    nodes.Add(new BuiltInNode(builtIn));
        //foreach (var clause in pred.Clauses)
        //{

        //}
        //if (nodes.Count == 0)
        //    throw new InvalidOperationException();
        //if (nodes.Count == 1)
        //    return nodes.Single();
        //return new SequenceNode([..nodes]);
        return default;
    }
}

//public static class CompilerUtils
//{
//    public record class ResolveGoalState(
//            ErgoDependencyGraph Graph,
//            Signature Signature,
//            Dictionary<Signature, CyclicalCallNode> CyclicalCallMap
//    )
//    {
//        public Action<ExecutionNode> OnReturn { get; set; } = _ => { };
//        public Maybe<Atom> CallerModule { get; }
//    }

//    ExecutionNode Recurse(ITerm goal, ICompilePredicateStep.Env env)
//        => Run(goal, env).GetAOrThrow();

//    Maybe<ExecutionNode> ResolveBase(ITerm goal)
//    {
//        if (goal is Variable v)
//            return new VariableNode(v);
//        if (goal is Atom { Value: true })
//            return new TrueNode();
//        if (goal is Atom { Value: false })
//            return new FalseNode();
//        if (goal.Equals(WellKnown.Literals.Cut))
//            return new CutNode();
//        return default;
//    }

//    Maybe<ExecutionNode> ResolveTuple(ITerm goal, ICompilePredicateStep.Env env)
//    {
//        if (goal is NTuple tup)
//        {
//            var list = tup.Contents.Select(x => Recurse(x, env))
//                .ToList();
//            if (list.Count == 0)
//                return TrueNode.Instance;
//            if (list.Count == 1)
//                return list[0];
//            return new SequenceNode(list);
//        }
//        return default;
//    }

//    Maybe<ExecutionNode> ResolveBranch(ITerm goal, ICompilePredicateStep.Env env)
//    {
//        if (goal is Complex { Functor: var functor, Arity: 2, Arguments: var args })
//        {
//            if (WellKnown.Operators.If.Synonyms.Contains(functor))
//                return new IfThenNode(
//                    Recurse(args[0], env),
//                    Recurse(args[1], env));
//            if (WellKnown.Operators.Disjunction.Synonyms.Contains(functor))
//            {
//                if (args[0] is Complex { Functor: var functor1, Arity: 2, Arguments: var args1 }
//                    && WellKnown.Operators.If.Synonyms.Contains(functor1))
//                {
//                    return new IfThenElseNode(
//                        Recurse(args1[0], env),
//                        Recurse(args1[1], env),
//                        Recurse(args[1], env));
//                }
//                return new BranchNode(
//                    Recurse(args[0], env),
//                    Recurse(args[1], env));
//            }
//        }
//        return default;
//    }

//    IEnumerable<ExecutionNode> ResolveQualifiedGoal(ITerm goal, ResolveGoalState state)
//    {
//        // Qualified match
//        if (state.Graph.Predicates.TryGetValue(state.Signature, out var node))
//            foreach (var n in ResolvePredicate(goal, node, state))
//                yield return n;
//        // Qualified variadic match
//        if (state.Graph.Predicates.TryGetValue(state.Signature.WithArity(default), out node))
//            yield return new BuiltInNode(state.Graph, node, goal);
//    }

//    IEnumerable<ExecutionNode> ResolveUnqualifiedGoal(ITerm goal, ResolveGoalState state)
//    {
//        var sig = state.Signature;
//        // Resolve all possible callees
//        if (!sig.Module.TryGetValue(out var module))
//        {
//            foreach (var possibleQualif in state.Graph.ModuleTree.Modules.Keys)
//            {
//                sig = sig.WithModule(possibleQualif);
//                // Match
//                if (state.Graph.Predicates.TryGetValue(sig, out var node))
//                    foreach (var n in ResolvePredicate(goal.Qualified(possibleQualif), node, state))
//                        yield return n;
//                // Variadic match
//                if (state.Graph.Predicates.TryGetValue(sig.WithArity(default), out node))
//                    yield return new BuiltInNode(state.Graph, node, goal);
//                // Dynamic match
//                if (state.Graph.ModuleTree.Modules[possibleQualif].GetMetaTableEntry(sig) is { IsDynamic: true })
//                    yield return new DynamicNode(goal.Qualified(possibleQualif));
//            }
//        }
//    }

//    IEnumerable<ExecutionNode> ResolveDynamicGoal(ITerm goal, ResolveGoalState state)
//    {
//        if (state.Signature.Module.TryGetValue(out var module))
//        {
//            // Qualified dynamic match
//            if (state.Graph.ModuleTree.Modules[module].GetMetaTableEntry(state.Signature) is { IsDynamic: true })
//                yield return new DynamicNode(goal);
//        }
//    }

//    Maybe<ExecutionNode> ResolveGoal(ITerm goal, ICompilePredicateStep.Env env)
//    {
//        if (!env.DependencyGraph.TryGetValue(out var graph))
//            throw new InvalidOperationException();
//        var state = new ResolveGoalState(graph, goal.GetSignature(), []);
//        var matches = ResolveQualifiedGoal(goal, state)
//            .Concat(ResolveUnqualifiedGoal(goal, state))
//            .Concat(ResolveDynamicGoal(goal, state))
//            .DefaultIfEmpty(FalseNode.Instance);
//        var node = matches.Aggregate((a, b) => new BranchNode(a, b));
//        state.OnReturn(node);
//        return node;
//    }


//    IEnumerable<ExecutionNode> ResolvePredicate(ITerm goal, PredicateDefinition pred, ResolveGoalState state)
//    {
//        if (state.CyclicalCallMap.TryGetValue(state.Signature, out var cyclical))
//        {
//            yield return new CyclicalCallNode(goal) { Clause = cyclical.Clause, Ref = cyclical.Ref };
//            yield break;
//        }
//        if (pred.Clauses.Any(c => c.IsCyclical || c.IsTailRecursive))
//            state.CyclicalCallMap[state.Signature] = new CyclicalCallNode(goal);
//        if (pred.BuiltIn.TryGetValue(out var builtin))
//            yield return new BuiltInNode(state.Graph, pred, goal);
//        var newMatches = new List<ExecutionNode>();
//        foreach (var clause in pred.Clauses.OrderByDescending(SortClausesBy))
//        {
//            var caller = state.CallerModule.GetOr(clause.DeclaringModule);
//            if (ResolveClause(goal, clause, state, caller).TryGetValue(out var match))
//                yield return match;
//        }
//        if (state.CyclicalCallMap.TryGetValue(state.Signature, out var c))
//            state.OnReturn += ret => c.Ref.Node = ret;
//        int SortClausesBy(ClauseDefinition x) => state.Graph.ModuleTree.Modules
//            .TryGetValue(x.DeclaringModule, out var m)
//            ? m.LoadOrder
//            : 0;
//    }

//    Maybe<ExecutionNode> ResolveClause(ITerm goal, ClauseDefinition clause, ResolveGoalState state, Atom callerModule)
//    {
//        var facts = new List<Clause>();
//        goal.GetQualification(out var head);
//        var module = state.Graph.ModuleTree.Modules[clause.DeclaringModule];
//        var sig = clause.Head.Qualified(module.Name).GetSignature();
//        HandleMetaAttributes(ref head);
//        return default;
//        //var substitutedClause = clause.Instantiate(ctx);
//        //substitutedClause.Head.GetQualification(out var clauseHead);
//        //if (!head.Unify(clauseHead).TryGetValue(out var subs))
//        //    return default;
//        //substitutedClause = substitutedClause.Substitute(subs);
//        //substitutedClause.Head.GetQualification(out clauseHead);
//        //var unif = Unify.MakeComplex(head, clauseHead);
//        //var unifDep = graph.Predicates[WellKnown.Signatures.Unify];
//        //var unifNode = new BuiltInNode(unifDep, unif);
//        //if (clause.IsFactual)
//        //    return unifNode;
//        //if (state.CyclicalCallMap.TryGetValue(sig, out var cycleNode))
//        //    cycleNode.Clause = clause;
//        //var execGraph = substitutedClause.ExecutionGraph
//        //    .GetOr(ToExecutionGraph(substitutedClause, graph, state.CyclicalCallMap));
//        //if (!head.Equals(clauseHead))
//        //{
//        //    return new SequenceNode([unifNode, execGraph.Root]);
//        //}
//        //return execGraph.Root;

//        void HandleMetaAttributes(ref ITerm head)
//        {
//            if (module.MetaTable.TryGetValue(sig, out var meta)
//                && meta.Arguments.TryGetValue(out var metaArgs))
//            {
//                var args = head.GetArguments();
//                for (int i = 0; i < metaArgs.Length; i++)
//                {
//                    args = args.SetItem(i, metaArgs[i] switch
//                    {
//                        '+' when !args[i].GetQualification(out _).HasValue => args[i].Qualified(callerModule),
//                        _ => args[i],
//                    });
//                }
//                if (head is Complex cplxHead)
//                    head = cplxHead.WithArguments(args);
//            }
//        }
//    }

//}