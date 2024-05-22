namespace Ergo.Runtime.BuiltIns;


public sealed class DictKeyValue : BuiltIn
{
    const int Arity = 3;
    public DictKeyValue()
        : base("", new($"dict_key_value"), Maybe<int>.Some(Arity), WellKnown.Modules.Dict)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (vm.Arg(0) is Variable)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, vm.Arg(0).Explain());
            return;
        }

        if (vm.Arg(0) is Dict dict)
        {
            if (!dict.Dictionary.Keys.Any())
            {
                vm.Fail();
                return;
            }
            var anyKey = false;
            var anyValue = false;
            foreach (var key in dict.Dictionary.Keys)
            {
                var s1 = LanguageExtensions.Unify(vm.Arg(1), key).TryGetValue(out var subs);
                if (s1)
                {
                    anyKey = true;
                    var s2 = LanguageExtensions.Unify(vm.Arg(2), dict.Dictionary[key]).TryGetValue(out var vSubs);
                    if (s2)
                    {
                        anyValue = true;
                        vm.Solution(SubstitutionMap.MergeRef(vSubs, subs));
                    }
                    else
                    {
                        vm.Fail();
                        return;
                    }
                }
            }

            if (!anyKey)
            {
                vm.Throw(ErgoVM.ErrorType.KeyNotFound, vm.Arg(0).Explain(), vm.Arg(1).Explain());
                return;
            }

            if (!anyValue)
            {
                vm.Fail();
                return;
            }
        }
        else vm.Fail();
    };
}
