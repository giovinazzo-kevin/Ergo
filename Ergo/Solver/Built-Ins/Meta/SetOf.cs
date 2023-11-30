using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class SetOf : SolutionAggregationBuiltIn
{
    public SetOf()
           : base("", new("setof"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        var any = false;
        foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(vm, args))
        {
            var env = vm.CloneEnvironment();
            var argSet = new Set(ArgVars.Contents, ArgVars.Scope);
            var setVars = new Set(ListVars.Contents, ArgVars.Scope);
            var setTemplate = new Set(ListTemplate.Contents, ArgVars.Scope);
            ErgoVM.Goals.Unify([setVars, argSet])(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            ErgoVM.Goals.Unify([args[2], setTemplate])(vm);
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
