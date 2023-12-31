
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
        var arg = node.Goal.GetArguments()[0].ToExecutionNode(node.Node.Graph, ctx: new("NOT")).Optimize();
        var op = arg.Compile();
        return new VirtualNode(vm =>
        {
            var newVm = vm.ScopedInstance();
            newVm.Query = op;
            newVm.Run();
            if (newVm.Solutions.Any())
                vm.Fail();
        });
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var newVm = vm.ScopedInstance();
        newVm.Query = ErgoVM.Ops.Goal(vm.Arg(0));
        newVm.Run();
        if (newVm.Solutions.Any())
            vm.Fail();
    };
}
