using Ergo.VM.BuiltIns;

namespace Ergo.Lang.Compiler;

public class BuiltInNode : GoalNode
{
    public BuiltIn BuiltIn { get; }
    public readonly ErgoVM.Goal BuiltInGoal;
    public readonly ITerm Head;
    public BuiltInNode(DependencyGraphNode node, ITerm goal, BuiltIn builtIn) : base(node, goal)
    {
        BuiltIn = builtIn;
        BuiltInGoal = ErgoVM.Goals.BuiltIn(BuiltIn);
        RuntimeHelpers.PrepareDelegate(BuiltInGoal);
        Goal.GetQualification(out Head);
    }

    public override ErgoVM.Op Compile() => vm => BuiltInGoal(Head.Substitute(vm.Environment).GetArguments())(vm);

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
        if (IsGround) return this;
        return new BuiltInNode(Node, Goal.Instantiate(ctx, vars), BuiltIn);
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new BuiltInNode(Node, Goal.Substitute(s), BuiltIn);
    }
}
