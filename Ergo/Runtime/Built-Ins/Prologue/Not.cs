
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

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

    public override ErgoVM.Op Compile() => vm =>
    {
        var newVm = vm.ScopedInstance();
        newVm.Query = vm.CompileQuery(new(vm.Arg(0)));
        newVm.Run();
        if (newVm.Solutions.Any())
            vm.Fail();
    };
}
