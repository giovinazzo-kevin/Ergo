using Ergo.Lang.Compiler;
using Ergo.Solver;

public class ExecutionNode
{
    public virtual ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return this;
    }
    public virtual ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return this;
    }
}

/// <summary>
/// Represents a cut, which prevents further backtracking.
/// </summary>
public class CutNode : ExecutionNode { }

/// <summary>
/// Represents an individual qualified goal. It might still be made up of multiple clauses, but only from one module's definition.
/// Ambiguous goals are represented as BranchNodes containing all possible qualifications of each goal.
/// </summary>
public class GoalNode : ExecutionNode
{
    public GoalNode(DependencyGraphNode node, ITerm goal)
    {
        Node = node;
        Goal = goal;
    }

    public ITerm Goal { get; }
    public DependencyGraphNode Node { get; }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new GoalNode(Node, Goal.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new GoalNode(Node, Goal.Substitute(s));
    }
}
public class VariadicGoalNode : GoalNode
{
    public VariadicGoalNode(DependencyGraphNode node, ITerm goal) : base(node, goal) { }
}
/// <summary>
/// Represents a goal that could not be resolved at compile time.
/// </summary>
public class DynamicNode : ExecutionNode
{
    public DynamicNode(ITerm goal)
    {
        Goal = goal;
    }

    public ITerm Goal { get; }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new DynamicNode(Goal.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new DynamicNode(Goal.Substitute(s));
    }
}

public class IfThenNode : ExecutionNode
{
    public IfThenNode(ExecutionNode condition, ExecutionNode trueBranch)
    {
        Condition = condition;
        TrueBranch = trueBranch;
    }

    public ExecutionNode Condition { get; }
    public ExecutionNode TrueBranch { get; }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new IfThenNode(Condition.Instantiate(ctx, vars), TrueBranch.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new IfThenNode(Condition.Substitute(s), TrueBranch.Substitute(s));
    }
}
/// <summary>
/// Represents an if-then-else statement.
/// </summary>
public class IfThenElseNode : ExecutionNode
{
    public IfThenElseNode(ExecutionNode condition, ExecutionNode trueBranch, ExecutionNode falseBranch)
    {
        Condition = condition;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }

    public ExecutionNode Condition { get; }
    public ExecutionNode TrueBranch { get; }
    public ExecutionNode FalseBranch { get; }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new IfThenElseNode(Condition.Instantiate(ctx, vars), TrueBranch.Instantiate(ctx, vars), FalseBranch.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new IfThenElseNode(Condition.Substitute(s), TrueBranch.Substitute(s), FalseBranch.Substitute(s));
    }
}

/// <summary>
/// Represents a logical disjunction.
/// </summary>
public class BranchNode : ExecutionNode
{
    public BranchNode(ExecutionNode left, ExecutionNode right)
    {
        Left = left;
        Right = right;
    }

    public ExecutionNode Left { get; }
    public ExecutionNode Right { get; }
    public bool LeftTried { get; set; } = false;
    public bool RightTried { get; set; } = false;
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new BranchNode(Left.Instantiate(ctx, vars), Right.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new BranchNode(Left.Substitute(s), Right.Substitute(s));
    }
}

/// <summary>
/// Represents a logical conjunction.
/// </summary>
public class SequenceNode : ExecutionNode
{
    public SequenceNode(List<ExecutionNode> nodes) => Nodes = nodes;

    public List<ExecutionNode> Nodes { get; }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new SequenceNode(Nodes.Select(n => n.Instantiate(ctx, vars)).ToList());
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new SequenceNode(Nodes.Select(n => n.Substitute(s)).ToList());
    }
}

public class TrueNode : ExecutionNode { }
public class FalseNode : ExecutionNode { }
public class VariableNode : ExecutionNode
{
    public Variable Binding { get; private set; }

    public VariableNode(Variable v)
    {
        Binding = v;
    }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new VariableNode((Variable)Binding.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        var term = ((ITerm)Binding).Substitute(s);
        if (term is not Variable)
            return new DynamicNode(term);
        return new VariableNode((Variable)term);
    }
}

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

    public IEnumerable<Solution> Execute(SolverContext ctx, SolverScope scope, SubstitutionMap currentSubstitutions = null)
    {
        var stack = new Stack<(ExecutionNode, SubstitutionMap)>();
        ExecutionNode current = Root;
        currentSubstitutions ??= new();

        while (current != null)
        {
            switch (current)
            {
                case TrueNode _:
                    yield return new Solution(scope, currentSubstitutions);
                    break;

                case FalseNode _:
                when_false:
                    if (stack.Count == 0) yield break;
                    (current, currentSubstitutions) = stack.Pop();
                    continue;

                case CutNode _:
                    stack.Clear();  // For the cut behavior, clearing the stack prevents further backtracking.
                    break;

                case SequenceNode seq:
                    for (int i = seq.Nodes.Count - 1; i >= 0; i--)
                    {
                        stack.Push((seq.Nodes[i], new SubstitutionMap(currentSubstitutions)));  // Deep copy
                    }
                    (current, currentSubstitutions) = stack.Pop();
                    continue;

                case BranchNode branch:
                    if (!branch.LeftTried)
                    {
                        branch.LeftTried = true;
                        stack.Push((branch.Right, new SubstitutionMap(currentSubstitutions)));  // Deep copy
                        current = branch.Left;
                    }
                    else if (!branch.RightTried)
                    {
                        branch.RightTried = true;
                        current = branch.Right;
                    }
                    else
                    {
                        if (stack.Count == 0) yield break;
                        (current, currentSubstitutions) = stack.Pop();
                    }
                    continue;
                case GoalNode goal:
                    var term = goal.Goal.Substitute(currentSubstitutions);
                    foreach (var clause in goal.Node.Clauses)
                    {
                        if (term.Unify(clause.Head).TryGetValue(out var subs))
                        {
                            SubstitutionMap.MergeRef(currentSubstitutions, subs);
                        }
                        if (clause.BuiltIn.TryGetValue(out var builtIn))
                        {
                            foreach (var eval in builtIn.Apply(ctx, scope, term.GetArguments()))
                            {
                                if (!eval.Result)
                                    goto when_false;
                                else
                                    yield return new(scope, eval.Substitutions);
                            }
                            continue;
                        }
                        if (clause.ExecutionGraph.TryGetValue(out var graph))
                        {
                            stack.Push((graph.Root, new SubstitutionMap(currentSubstitutions)));  // Deep copy
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported execution node: {current.GetType().Name}");
            }

            if (stack.Count == 0) break;  // If we've exhausted the graph without a solution.
            (current, currentSubstitutions) = stack.Pop();
        }
    }
}

public static class ExecutionGraphExtensions
{
    public static ExecutionGraph ToExecutionGraph(this Predicate clause, DependencyGraph graph)
    {
        //var ctx = new InstantiationContext("_C");
        var scope = graph.Scope.WithCurrentModule(clause.DeclaringModule);
        return new(ToExecutionNode(clause.Body));

        ExecutionNode ToExecutionNode(ITerm goal)
        {
            //goal = goal.Instantiate(ctx);
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
                if (arity == 2 && WellKnown.Operators.Disjunction.Synonyms.Contains(functor))
                    return new BranchNode(ToExecutionNode(args[0]), ToExecutionNode(args[1]));
                if (arity == 2 && WellKnown.Operators.If.Synonyms.Contains(functor))
                {
                    if (args[1] is Complex { Functor: var functor1, Arity: var arity1, Arguments: var args1 }
                        && arity1 == 2 && WellKnown.Operators.Disjunction.Synonyms.Contains(functor1))
                    {
                        return new IfThenElseNode(ToExecutionNode(args[0]), ToExecutionNode(args1[0]), ToExecutionNode(args1[1]));
                    }
                    return new IfThenNode(ToExecutionNode(args[0]), ToExecutionNode(args[1]));
                }
            }
            // If 'goal' isn't any other type of node, then it's a proper goal and we need to resolve it in the context of 'clause'.
            var matches = new List<ExecutionNode>();
            var sig = goal.GetSignature();
            // Qualified match
            if (graph.GetNode(sig).TryGetValue(out var node))
                matches.Add(new GoalNode(node, goal));
            // Resolve all possible callees
            if (!sig.Module.TryGetValue(out var module))
            {
                foreach (var possibleQualif in scope.VisibleModules)
                {
                    sig = sig.WithModule(possibleQualif);
                    if (graph.GetNode(sig).TryGetValue(out node))
                        matches.Add(new GoalNode(node, goal));
                    // Variadic match
                    if (graph.GetNode(sig.WithArity(default)).TryGetValue(out node))
                        matches.Add(new VariadicGoalNode(node, goal));
                    // Dynamic match
                    if (scope.Modules[possibleQualif].DynamicPredicates.Contains(sig.WithModule(default)))
                        matches.Add(new DynamicNode(goal));
                }
            }
            if (matches.Count == 0)
            {
                throw new CompilerException(ErgoCompiler.ErrorType.UnresolvedPredicate, goal.GetSignature().Explain());
            }
            return new SequenceNode(matches);
        }
    }
}