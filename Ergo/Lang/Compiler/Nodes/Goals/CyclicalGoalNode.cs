namespace Ergo.Lang.Compiler;

public class CyclicalGoalNode : GoalNode
{
    public CyclicalGoalNode(DependencyGraphNode node, ITerm goal) : base(node, goal) { }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new CyclicalGoalNode(Node, Goal.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new CyclicalGoalNode(Node, Goal.Substitute(s));
    }
}
