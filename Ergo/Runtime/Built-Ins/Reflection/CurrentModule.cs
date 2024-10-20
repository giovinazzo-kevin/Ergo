namespace Ergo.Runtime.BuiltIns;

public sealed class CurrentModule : ErgoBuiltIn
{
    public CurrentModule()
        : base("", new("current_module"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override Op Compile()
    {
        return vm =>
        {
            vm.SetArg(1, vm.KB.Scope.Entry);
            ErgoVM.Goals.Unify2(vm);
        };
    }
}
