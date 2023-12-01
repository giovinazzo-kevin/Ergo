

using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Compare : BuiltIn
{
    public Compare()
        : base("", new("compare"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        var cmp = args[1].CompareTo(args[2]);
        if (args[0].IsGround)
        {
            if (!args[0].Matches<int>(out var result))
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[0].Explain());
                return;
            }

            if (!result.Equals(cmp))
                vm.Fail();
            return;
        }
        ErgoVM.Goals.Unify([args[0], new Atom(cmp)])(vm);
    };
}
