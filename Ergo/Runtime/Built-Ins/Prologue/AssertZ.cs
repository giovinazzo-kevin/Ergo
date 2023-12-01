using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class AssertZ : DynamicPredicateBuiltIn
{
    public AssertZ()
        : base("", new("assertz"), 1)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        if (!Assert(vm, args[0], z: true))
            vm.Fail();
    };
}
