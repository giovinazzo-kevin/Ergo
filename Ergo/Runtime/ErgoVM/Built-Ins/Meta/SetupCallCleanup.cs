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
        var args = vm.Args2;
        var newVm = vm.ScopedInstance();
        var setup = CallInst.Compile();
        newVm.Query = ErgoVM.Ops.And2(setup, ErgoVM.Ops.Cut);
        newVm.Arity = vm.Arity = 2;
        newVm.SetArg2(1, args[1]);
        newVm.Run();
        if (!newVm.TryPopSolution(out _))
        {
            vm.Fail();
            return;
        }
        vm.SetArg2(1, args[2]);
        CallInst.Compile()(vm);
        if (vm.State != ErgoVM.VMState.Fail)
        {
            vm.Arity = 2;
            vm.SetArg2(1, args[3]);
            var sols = vm.NumSolutions;
            CallInst.Compile()(vm);
            while (vm.NumSolutions > sols)
                vm.TryPopSolution(out _);
        }
    };
}
