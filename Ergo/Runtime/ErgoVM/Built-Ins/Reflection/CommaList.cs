namespace Ergo.Runtime.BuiltIns;

public sealed class CommaToList : BuiltIn
{
    public CommaToList()
        : base("", "comma_list", Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var (commaArg, listArg) = (vm.Arg(0), vm.Arg(1));
        if (listArg is not Variable)
        {
            if (listArg is not List list)
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.List, listArg.Explain());
                return;
            }
            var comma = new NTuple(list.Contents, default);
            vm.SetArg(0, commaArg);
            vm.SetArg(1, comma);
            ErgoVM.Goals.Unify2(vm);
            return;
        }

        if (commaArg is not Variable)
        {
            if (commaArg is not NTuple comma)
                comma = new NTuple([commaArg]);
            var list = new List(comma.Contents, default, default);
            vm.SetArg(0, listArg);
            vm.SetArg(1, list);
            ErgoVM.Goals.Unify2(vm);
            return;
        }
        vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, WellKnown.Types.List, commaArg.Explain());
    };
}
