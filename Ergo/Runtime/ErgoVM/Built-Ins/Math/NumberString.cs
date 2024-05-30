using PeterO.Numbers;

namespace Ergo.Runtime.BuiltIns;

public sealed class NumberString : BuiltIn
{
    public NumberString()
        : base("", "number_string", Maybe<int>.Some(2), WellKnown.Modules.Math)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var (str, num) = (vm.Arg(1), vm.Arg(0));
        if (!str.IsGround && !num.IsGround)
            return;
        else if (!str.IsGround && num.IsGround)
        {
            if (!str.Match(out EDecimal d))
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, num);
                return;
            }
            vm.SetArg(0, num);
            vm.SetArg(1, (Atom)d.ToString());
            ErgoVM.Goals.Unify2(vm);
        }
        else if (str.IsGround)
        {
            if (!str.Match(out string s))
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.String, num);
                return;
            }
            EDecimal n = null;
            try
            {
                n = EDecimal.FromString(s);
            }
            catch { }
            if (n == null)
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, num);
                return;
            }
            vm.SetArg(0, num);
            vm.SetArg(1, (Atom)n);
            ErgoVM.Goals.Unify2(vm);
        }
    };
}