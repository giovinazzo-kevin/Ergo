namespace Ergo.Runtime.BuiltIns;

public sealed class SetOf : SolutionAggregationBuiltIn
{
    public SetOf()
           : base("", new("setof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var any = false;
        foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(vm))
        {
            var state = vm.Memory.SaveState();
            var argSet = new Set(ArgVars.Contents, ArgVars.Scope);
            var setVars = new Set(ListVars.Contents, ArgVars.Scope);
            var setTemplate = new Set(ListTemplate.Contents, ArgVars.Scope);
            vm.SetArg2(1, vm.Memory.StoreTerm(setVars));
            vm.SetArg2(2, vm.Memory.StoreTerm(argSet));
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            vm.SetArg2(1, vm.Args2[3]);
            vm.SetArg2(2, vm.Memory.StoreTerm(setTemplate));
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
