
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class CommaToList : BuiltIn
{
    public CommaToList()
        : base("", new("comma_list"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Goal Compile() => args =>
    {
        var (commaArg, listArg) = (args[0], args[1]);
        return vm =>
        {
            if (listArg is not Variable)
            {
                if (listArg is not List list)
                {
                    vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.List, listArg.Explain());
                    return;
                }
                var comma = new NTuple(list.Contents, default);
                ErgoVM.Goals.Unify([commaArg, comma])(vm);
                return;
            }

            if (commaArg is not Variable)
            {
                if (commaArg is not NTuple comma)
                {
                    vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.CommaList, commaArg.Explain());
                    return;
                }
                var list = new List(comma.Contents, default, default);
                ErgoVM.Goals.Unify([listArg, list])(vm);
                return;
            }
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, WellKnown.Types.List, commaArg.Explain());
        };
    };
}
