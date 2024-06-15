using Ergo.Runtime.BuiltIns;
using System.Diagnostics;

namespace Ergo.Lang.Compiler;

public class BuiltInNode : GoalNode
{
    public BuiltIn BuiltIn { get; }
    protected ErgoVM.Op CompiledBuiltIn { get; private set; }
    public readonly ITerm Head;
    public readonly ImmutableArray<ITerm> Args;

    public BuiltInNode(DependencyGraphNode node, ITerm goal, BuiltIn builtIn, bool compile = true) : base(node, goal)
    {
        BuiltIn = builtIn;
        Goal.GetQualification(out Head);
        Args = Head.GetArguments();
        if (compile)
        {
            CompiledBuiltIn = BuiltIn.Compile();
            RuntimeHelpers.PrepareDelegate(CompiledBuiltIn);
        }
    }
    public override ErgoVM.Op Compile() => vm =>
    {
        if (!vm.Flag(VMFlags.TCO))
            vm.SetArgs2(vm.Memory[(StructureAddress)vm.Memory.StoreTerm(Head)]);
        vm.SetFlag(VMFlags.ContinuationIsDet, IsContinuationDet);
        vm.LogState(Explain(false));
        Debug.WriteLine(@$"--------- \CALL/ ---------");
        Debug.WriteLine(vm.DebugArgs.Select(a => a.Explain(false)).Join("\r\n"));
        CompiledBuiltIn(vm);
    };

    public override int OptimizationOrder => base.OptimizationOrder + BuiltIn.OptimizationOrder;
    public override bool IsDeterminate => BuiltIn.IsDeterminate(Args);
    public override int CheckSum => HashCode.Combine(BuiltIn.GetType(),
        Args.Aggregate(0, (a, b) => HashCode.Combine(b, a.GetHashCode())));
    public override ExecutionNode Optimize(OptimizationFlags flags)
    {
        return BuiltIn.Optimize(this);
    }
    public override List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes, OptimizationFlags flags)
    {
        return BuiltIn.OptimizeSequence(nodes, flags);
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
