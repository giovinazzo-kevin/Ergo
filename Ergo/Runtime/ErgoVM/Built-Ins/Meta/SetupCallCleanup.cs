namespace Ergo.Runtime.BuiltIns;

public sealed class SetupCallCleanup : BuiltIn
{
    public SetupCallCleanup()
        : base("", "setup_call_cleanup", 3, WellKnown.Modules.Meta)
    {
    }

    private readonly Call CallInst = new();
    public override ErgoVM.Op Compile() => vm =>
    {
        //var newVm = vm.ScopedInstance();
        var setup = CallInst.Compile();
        vm.Query = ErgoVM.Ops.And2(setup, ErgoVM.Ops.Cut);
        vm.Arity = vm.Arity = 2;
        var arg2 = vm.Arg2(2);
        var arg3 = vm.Arg2(3);
        vm.SetArg2(1, vm.Arg2(1));
        vm.Run();
        if (!vm.TryPopSolution(out _))
        {
            vm.Fail();
            return;
        }
        vm.SetArg2(1, arg2);
        CallInst.Compile()(vm);
        if (vm.State != ErgoVM.VMState.Fail)
        {
            vm.Arity = 2;
            vm.SetArg2(1, arg3);
            var sols = vm.NumSolutions;
            CallInst.Compile()(vm);
            while (vm.NumSolutions > sols)
                vm.TryPopSolution(out _);
        }
    };
}
