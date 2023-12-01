using Ergo.Lang.Compiler;
using PeterO.Numbers;

namespace Ergo.Runtime.BuiltIns;

public sealed class NumberString : BuiltIn
{
    public NumberString()
        : base("", new("number_string"), Maybe<int>.Some(2), WellKnown.Modules.Math)
    {
    }

    public override ErgoVM.Goal Compile() => arguments => vm =>
    {
        var (str, num) = (arguments[1], arguments[0]);
        if (!str.IsGround && !num.IsGround)
            return;
        else if (!str.IsGround && num.IsGround)
        {
            if (!str.Matches(out EDecimal d))
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, num);
                return;
            }
            ErgoVM.Goals.Unify([num, new Atom(d.ToString())])(vm);
        }
        else if (str.IsGround)
        {
            if (!str.Matches(out string s))
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
            ErgoVM.Goals.Unify([num, new Atom(n)])(vm);
        }
    };
}