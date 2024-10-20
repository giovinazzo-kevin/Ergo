namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents an individual qualified goal. It might still be made up of multiple clauses, but only from one module's definition.
/// </summary>
public abstract class GoalNode : DynamicNode
{
    public LegacyDependencyGraphNode Node { get; }
    public GoalNode(LegacyDependencyGraphNode node, ITerm goal)
        : base(goal)
    {
        Node = node;
    }
}
