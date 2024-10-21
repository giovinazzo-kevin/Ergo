using Ergo.Lang.Compiler;
using PeterO.Numbers;

namespace Ergo.Runtime.BuiltIns;

public sealed class Number : ErgoBuiltIn
{
    public override bool IsDeterminate(ImmutableArray<ITerm> args) => true;

    public Number()
        : base("", new("number"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override ExecutionNode Optimize(OldBuiltInNode node)
    {
        var args = node.Goal.GetArguments();
        if (args[0] is not Variable)
        {
            if (args[0] is not Atom { Value: EDecimal _ })
                return FalseNode.Instance; // this is probably a complex term, but definitely not a number
        }
        return node; // we don't know yet
    }

    public override Op Compile()
    {
        return vm =>
        {
            if (!(vm.Arg(0) is Atom { Value: EDecimal _ }))
                vm.Fail();
        };
    }
}
