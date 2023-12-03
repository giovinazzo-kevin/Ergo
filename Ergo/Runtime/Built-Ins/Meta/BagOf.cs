namespace Ergo.Runtime.BuiltIns;

public sealed class BagOf : SolutionAggregationBuiltIn
{
    public BagOf()
        : base("", new("bagof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var any = false;
        var args = vm.Args;
        foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(vm, args.ToImmutableArray()))
        {
            var env = vm.CloneEnvironment();
            vm.SetArg(0, ListVars);
            vm.SetArg(1, ArgVars);
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            vm.SetArg(0, args[2]);
            vm.SetArg(1, ListTemplate);
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            vm.Solution();
            ReleaseAndRestore();
            any = true;

            void ReleaseAndRestore()
            {
                SubstitutionMap.Pool.Release(vm.Environment);
                vm.Environment = env;
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
