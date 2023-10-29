namespace Ergo.Lang.Compiler;

public class TailRecursiveGoalNode : GoalNode
{
    public TailRecursiveGoalNode(DependencyGraphNode node, ITerm goal) : base(node, goal) { }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new TailRecursiveGoalNode(Node, Goal.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new TailRecursiveGoalNode(Node, Goal.Substitute(s));
    }
}
