using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class SetupCallCleanup : SolverBuiltIn
{
    public SetupCallCleanup()
        : base("", new("setup_call_cleanup"), 3, WellKnown.Modules.Meta)
    {
    }

    private readonly Call CallInst = new Call();
    public override ErgoVM.Goal Compile() => args => vm =>
    {
        var newVm = vm.CreateChild();
        var setup = CallInst.Compile()([args[0]]);
        newVm.Query = ErgoVM.Ops.And2(setup, ErgoVM.Ops.Cut);
        newVm.Run();
        if (!newVm.TryPopSolution(out var sol))
        {
            vm.Fail();
            return;
        }
        CallInst.Compile()([args[1]])(vm);
        if (vm.State != ErgoVM.VMState.Fail)
        {
            var sols = vm.NumSolutions;
            CallInst.Compile()([args[2]])(vm);
            while (vm.NumSolutions > sols)
                vm.TryPopSolution(out _);
        }
    };
}
