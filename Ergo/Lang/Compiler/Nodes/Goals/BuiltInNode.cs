using Ergo.Solver.BuiltIns;

namespace Ergo.Lang.Compiler;

public class BuiltInNode : GoalNode
{
    public SolverBuiltIn BuiltIn { get; }
    public BuiltInNode(DependencyGraphNode node, ITerm goal, SolverBuiltIn builtIn) : base(node, goal)
    {
        BuiltIn = builtIn;
    }

    public override ErgoVM.Op Compile() => ErgoVM.Ops.BuiltInGoal(Goal, BuiltIn);

    public override int OptimizationOrder => base.OptimizationOrder + BuiltIn.OptimizationOrder;
    public override ExecutionNode Optimize()
    {
        return BuiltIn.Optimize(this);
    }
    public override List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes)
    {
        return BuiltIn.OptimizeSequence(nodes);
    }

    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new BuiltInNode(Node, Goal.Instantiate(ctx, vars), BuiltIn);
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new BuiltInNode(Node, Goal.Substitute(s), BuiltIn);
    }
}
