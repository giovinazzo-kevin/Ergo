namespace Ergo.Runtime.BuiltIns;

public sealed class Compare : BuiltIn
{
    public Compare()
        : base("", "compare", Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args2;
        var cmp = vm.Memory.Dereference(args[2])
            .CompareTo(vm.Memory.Dereference(args[3]));
        var a0 = vm.Memory.Dereference(args[1]);
        if (a0.IsGround)
        {
            if (!a0.Match<int>(out var result))
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, a0.Explain());
                return;
            }

            if (!result.Equals(cmp))
                vm.Fail();
            return;
        }
        vm.SetArg(1, (Atom)cmp);
        ErgoVM.Goals.Unify2(vm);
    };
}
