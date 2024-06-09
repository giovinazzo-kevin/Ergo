namespace Ergo.Runtime.BuiltIns;

public sealed class CurrentModule : BuiltIn
{
    public CurrentModule()
        : base("", "current_module", Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile()
    {
        return vm =>
        {
            vm.SetArg(1, vm.CKB.Scope.Entry);
            ErgoVM.Goals.Unify2(vm);
        };
    }
}
