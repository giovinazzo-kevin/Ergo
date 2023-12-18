namespace Ergo.Runtime.BuiltIns;

public sealed class SetOf : SolutionAggregationBuiltIn
{
    public SetOf()
           : base("", new("setof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        var any = false;
        foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(vm, args.ToImmutableArray()))
        {
            var env = vm.CloneEnvironment();
            var argSet = new Set(ArgVars.Contents, ArgVars.Scope);
            var setVars = new Set(ListVars.Contents, ArgVars.Scope);
            var setTemplate = new Set(ListTemplate.Contents, ArgVars.Scope);
            vm.SetArg(0, setVars);
            vm.SetArg(1, argSet);
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            vm.SetArg(0, args[2]);
            vm.SetArg(1, setTemplate);
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
