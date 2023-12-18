namespace Ergo.Runtime.BuiltIns;

public sealed class SetupCallCleanup : BuiltIn
{
    public SetupCallCleanup()
        : base("", new("setup_call_cleanup"), 3, WellKnown.Modules.Meta)
    {
    }

    private readonly Call CallInst = new();
    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        var newVm = vm.Clone();
        var setup = CallInst.Compile();
        newVm.Query = ErgoVM.Ops.And2(setup, ErgoVM.Ops.Cut);
        newVm.Arity = vm.Arity = 1;
        newVm.SetArg(0, args[0]);
        newVm.Run();
        if (!newVm.TryPopSolution(out _))
        {
            vm.Fail();
            return;
        }
        vm.SetArg(0, args[1]);
        CallInst.Compile()(vm);
        if (vm.State != ErgoVM.VMState.Fail)
        {
            vm.Arity = 1;
            vm.SetArg(0, args[2]);
            var sols = vm.NumSolutions;
            CallInst.Compile()(vm);
            while (vm.NumSolutions > sols)
                vm.TryPopSolution(out _);
        }
    };
}
