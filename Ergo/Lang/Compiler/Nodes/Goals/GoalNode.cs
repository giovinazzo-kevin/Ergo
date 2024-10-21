using Ergo.Compiler;

namespace Ergo.Lang.Compiler;

/// <summary>
/// Represents an individual qualified goal. It might still be made up of multiple clauses, but only from one module's definition.
/// </summary>
public abstract class GoalNode : DynamicNode
{
    public ErgoDependencyGraph DependencyGraph { get; set; }
    public PredicateDefinition Definition { get; }
    public GoalNode(ErgoDependencyGraph graph, PredicateDefinition node, ITerm goal)
        : base(goal)
    {
        DependencyGraph = graph;
        Definition = node;
    }
}
