namespace Ergo.Runtime.BuiltIns;

public sealed class Compare : BuiltIn
{
    public Compare()
        : base("", new("compare"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        var cmp = args[1].CompareTo(args[2]);
        if (args[0].IsGround)
        {
            if (!args[0].Match<int>(out var result))
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[0].Explain());
                return;
            }

            if (!result.Equals(cmp))
                vm.Fail();
            return;
        }
        vm.SetArg(1, new Atom(cmp));
        ErgoVM.Goals.Unify2(vm);
    };
}
