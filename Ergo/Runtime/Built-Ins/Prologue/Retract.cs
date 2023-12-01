using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Retract : DynamicPredicateBuiltIn
{
    public Retract()
        : base("", new("retract"), 1)
    {
    }


    public override ErgoVM.Goal Compile() => args =>
    {
        return vm =>
        {
            RetractOp(vm);
        };
        void RetractOp(ErgoVM vm)
        {
            if (Retract(vm, args[0], all: false))
            {
                vm.PushChoice(RetractOp);
                vm.Solution();
            }
            else vm.Fail();
        }
    };
}
