using Ergo.Lang.Compiler;

namespace Ergo.VM.BuiltIns;

public sealed class AssertA : DynamicPredicateBuiltIn
{
    public AssertA()
        : base("", new("asserta"), 1)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        if (!Assert(vm, args[0], z: false))
            vm.Fail();
    };
}
