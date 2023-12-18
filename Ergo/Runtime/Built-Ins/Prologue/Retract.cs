namespace Ergo.Runtime.BuiltIns;

public sealed class Retract : DynamicPredicateBuiltIn
{
    public Retract()
        : base("", new("retract"), 1)
    {
    }


    public override ErgoVM.Op Compile()
    {
        return RetractOp;
        void RetractOp(ErgoVM vm)
        {
            if (Retract(vm, vm.Arg(0), all: false))
            {
                vm.PushChoice(RetractOp);
                vm.Solution();
            }
            else vm.Fail();
        }
    }
}
