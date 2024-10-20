using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Ground : ErgoBuiltIn
{
    public Ground()
        : base("", new("ground"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override ExecutionNode Optimize(BuiltInNode node) =>
        node.Goal.IsGround ? TrueNode.Instance : node;
    public override Op Compile()
    {
        return vm =>
        {
            if (!(vm.Arg(0).IsGround))
                vm.Fail();
        };
    }
}
