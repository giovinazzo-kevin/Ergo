namespace Ergo.Runtime.BuiltIns;

public sealed class RetractAll : DynamicPredicateBuiltIn
{
    public RetractAll()
        : base("", new("retractall"), 1)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (Retract(vm, vm.Arg(0), all: true))
        {
            vm.Solution();
        }
        else vm.Success();
    };
}
