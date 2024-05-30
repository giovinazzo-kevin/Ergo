namespace Ergo.Runtime.BuiltIns;

public sealed class BagOf : SolutionAggregationBuiltIn
{
    public BagOf()
        : base("", "bagof", 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var any = false;
        foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(vm))
        {
            var state = vm.Memory.SaveState();
            vm.SetArg2(1, vm.Memory.StoreTerm(ListVars));
            vm.SetArg2(2, vm.Memory.StoreTerm(ArgVars));
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            vm.SetArg2(1, vm.Args2[3]);
            vm.SetArg2(2, vm.Memory.StoreTerm(ListTemplate));
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            vm.Solution();
            ReleaseAndRestore();
            any = true;

            void ReleaseAndRestore()
            {
                vm.Memory.LoadState(state);
            }
            bool ReleaseAndRestoreEarlyReturn()
            {
                if (vm.State == ErgoVM.VMState.Fail)
                {
                    ReleaseAndRestore();
                    return true;
                }
                return false;
            }
        }

        if (!any)
        {
            vm.Fail();
        }
    };
}
