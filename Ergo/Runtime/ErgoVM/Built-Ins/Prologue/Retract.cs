﻿namespace Ergo.Runtime.BuiltIns;

public sealed class Retract : DynamicPredicateBuiltIn
{
    public Retract()
        : base("", "retract", 1)
    {
    }


    public override ErgoVM.Op Compile()
    {
        return RetractOp;
        static void RetractOp(ErgoVM vm)
        {
            if (Retract(vm, vm.Arg2(1), all: false))
            {
                vm.PushChoice(RetractOp);
                vm.Solution();
            }
            else vm.Fail();
        }
    }
}
