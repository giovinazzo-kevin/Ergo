using Ergo.Runtime.BuiltIns;

namespace Ergo.Lang.Compiler;

public class BuiltInNode : GoalNode
{
    public BuiltIn BuiltIn { get; }
    public readonly ErgoVM.Op BuiltInGoal;
    public readonly IReadOnlyList<ITerm> Args;
    public BuiltInNode(DependencyGraphNode node, ITerm goal, BuiltIn builtIn) : base(node, goal)
    {
        BuiltIn = builtIn;
        Goal.GetQualification(out var head);
        Args = head.GetArguments();
        var compiled = BuiltIn.Compile();
        BuiltInGoal = vm =>
        {
            vm.Arity = Args.Count;
            for (int i = 0; i < Args.Count; i++)
                vm.SetArg(i, Args[i].Substitute(vm.Environment));
            compiled(vm);
        };
        RuntimeHelpers.PrepareDelegate(compiled);
        RuntimeHelpers.PrepareDelegate(BuiltInGoal);
    }

    public override ErgoVM.Op Compile() => BuiltInGoal;

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
