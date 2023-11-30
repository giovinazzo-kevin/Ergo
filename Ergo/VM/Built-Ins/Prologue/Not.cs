
using Ergo.Lang.Compiler;

namespace Ergo.VM.BuiltIns;

public sealed class Not : BuiltIn
{
    public Not()
        : base("", new("not"), Maybe<int>.Some(1), WellKnown.Modules.Prologue)
    {
    }

    public override ExecutionNode Optimize(BuiltInNode node)
    {
        if (!node.Goal.IsGround)
            return node;
        var arg = node.Goal.GetArguments()[0].ToExecutionNode(node.Node.Graph, ctx: new("NOT")).Optimize();
        if (arg is TrueNode)
            return FalseNode.Instance;
        if (arg is FalseNode)
            return TrueNode.Instance;
        return node;
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        var newVm = vm.CreateChild();
        newVm.Query = ErgoVM.Ops.Goal(args.Single());
        newVm.Run();
        if (newVm.Solutions.Any())
            vm.Fail();
    };
}
