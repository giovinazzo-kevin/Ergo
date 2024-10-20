using Ergo.Runtime.BuiltIns;

namespace Ergo.Lang.Compiler;

public class BuiltInNode : GoalNode
{
    public ErgoBuiltIn BuiltIn { get; }
    protected Op CompiledBuiltIn { get; private set; }
    public readonly ImmutableArray<ITerm> Args;

    public BuiltInNode(DependencyGraphNode node, ITerm goal, ErgoBuiltIn builtIn, bool compile = true) : base(node, goal)
    {
        BuiltIn = builtIn;
        Goal.GetQualification(out var head);
        Args = head.GetArguments();
        if (compile)
        {
            CompiledBuiltIn = BuiltIn.Compile();
            RuntimeHelpers.PrepareDelegate(CompiledBuiltIn);
        }
    }
    public static Op SetArgs(ImmutableArray<ITerm> args) => vm =>
    {
        vm.Arity = args.Length;
        for (int i = 0; i < args.Length; i++)
            vm.SetArg(i, args[i].Substitute(vm.Environment));
    };

    public override Op Compile() => vm =>
    {
        SetArgs(Args)(vm);
        vm.SetFlag(VMFlags.ContinuationIsDet, IsContinuationDet);
        vm.LogState(Explain(false));
        CompiledBuiltIn(vm);
    };

    public override int OptimizationOrder => base.OptimizationOrder + BuiltIn.OptimizationOrder;
    public override bool IsDeterminate => BuiltIn.IsDeterminate(Args);
    public override int CheckSum => HashCode.Combine(BuiltIn.GetType(),
        Args.Aggregate(0, (a, b) => HashCode.Combine(b, a.GetHashCode())));
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
        return new BuiltInNode(Node, Goal.Instantiate(ctx, vars), BuiltIn, compile: false)
        {
            CompiledBuiltIn = CompiledBuiltIn
        };
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        if (IsGround) return this;
        return new BuiltInNode(Node, Goal.Substitute(s), BuiltIn, compile: false)
        {
            CompiledBuiltIn = CompiledBuiltIn
        };
    }
}
