namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents an individual qualified goal. It might still be made up of multiple clauses, but only from one module's definition.
/// </summary>
public abstract class GoalNode : DynamicNode
{
    public DependencyGraphNode Node { get; }
    public GoalNode(DependencyGraphNode node, ITerm goal)
        : base(goal)
    {
        Node = node;
    }
}
