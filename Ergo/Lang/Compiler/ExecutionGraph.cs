using Ergo.Solver.BuiltIns;

public class ExecutionNode { }

/// <summary>
/// Represents a cut, which prevents further backtracking.
/// </summary>
public class CutNode : ExecutionNode { }

/// <summary>
/// Represents a built-in node, similar to a goal node but more primitive and closely integrated with C#.
/// </summary>
public class BuiltInNode : ExecutionNode
{
    public SolverBuiltIn BuiltIn { get; }
}

/// <summary>
/// Represents an individual qualified goal. It might still be made up of multiple clauses, but only from one module's definition.
/// Ambiguous goals are represented as BranchNodes containing all possible qualifications of each goal.
/// </summary>
public class GoalNode : ExecutionNode
{
    public GoalNode(DependencyGraphNode goal) => Goal = goal;

    public DependencyGraphNode Goal { get; }
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

public static class ExecutionGraphExtensions
{
    public static IEnumerable<ExecutionNode> ToExecutionGraph(this Predicate clause, DependencyGraph graph)
    {
        foreach (var goal in clause.Body.Contents)
        {
        }
        return null;
        static ExecutionNode ToExecutionNode(ITerm goal)
        {
            if (goal is NTuple tup)
                return new SequenceNode(tup.Contents.Select(x => ToExecutionNode(x)).ToList());
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
            return null;
        }
    }
}