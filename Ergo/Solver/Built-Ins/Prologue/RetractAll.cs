using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class RetractAll : DynamicPredicateBuiltIn
{
    public RetractAll()
        : base("", new("retractall"), 1)
    {
    }

    public override ErgoVM.Goal Compile() => args =>
    {
        return vm =>
        {
            if (Retract(vm, args[0], all: true))
            {
                vm.Solution();
            }
            else vm.Fail();
        };
    };
}
