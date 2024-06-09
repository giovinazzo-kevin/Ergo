namespace Ergo.Runtime.BuiltIns;

public sealed class RetractAll : DynamicPredicateBuiltIn
{
    public RetractAll()
        : base("", "retractall", 1)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (Retract(vm, vm.Arg2(1), all: true))
        {
            vm.Solution();
        }
        else vm.Success();
    };
}
