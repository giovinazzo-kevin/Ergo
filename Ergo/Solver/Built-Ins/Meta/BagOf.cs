using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class BagOf : SolutionAggregationBuiltIn
{
    public BagOf()
        : base("", new("bagof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        var any = false;
        foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(vm, args))
        {
            var env = vm.CloneEnvironment();
            ErgoVM.Goals.Unify([ListVars, ArgVars])(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            ErgoVM.Goals.Unify([args[2], ListTemplate])(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            vm.Solution();
            ReleaseAndRestore();
            any = true;

            void ReleaseAndRestore()
            {
                Substitution.Pool.Release(vm.Environment);
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
