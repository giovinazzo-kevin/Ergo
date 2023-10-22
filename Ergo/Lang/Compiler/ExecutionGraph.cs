using Ergo.Lang.Compiler;
using Ergo.Solver;

public class ExecutionNode { }

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
}

/// <summary>
/// Represents a logical conjunction.
/// </summary>
public class SequenceNode : ExecutionNode
{
    public SequenceNode(List<ExecutionNode> nodes) => Nodes = nodes;

    public List<ExecutionNode> Nodes { get; }
}

public class TrueNode : ExecutionNode { }
public class FalseNode : ExecutionNode { }
public class VariableNode : ExecutionNode
{
    public readonly Variable Binding;

    public VariableNode(Variable v)
    {
        Binding = v;
    }
}

public sealed class ExecutionGraph
{
    public SequenceNode Root;

    public ExecutionGraph(SequenceNode root) => Root = root;

    public IEnumerable<Solution> Execute(SolverContext ctx, SolverScope scope)
    {
        var stack = new Stack<(ExecutionNode, SubstitutionMap)>();
        ExecutionNode current = Root;
        SubstitutionMap currentSubstitutions = new();

        while (current != null)
        {
            switch (current)
            {
                case TrueNode _:
                    yield return new Solution(scope, currentSubstitutions);
                    break;

                case FalseNode _:
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
        var scope = graph.Scope.WithCurrentModule(clause.DeclaringModule);
        var seq = new SequenceNode(new());
        foreach (var goal in clause.Body.Contents)
            seq.Nodes.Add(ToExecutionNode(goal));
        return new(seq);

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