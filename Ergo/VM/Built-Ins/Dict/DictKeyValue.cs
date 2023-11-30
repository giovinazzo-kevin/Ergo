
using Ergo.Lang.Compiler;

namespace Ergo.VM.BuiltIns;


public sealed class DictKeyValue : BuiltIn
{
    public DictKeyValue()
        : base("", new($"dict_key_value"), Maybe<int>.Some(3), WellKnown.Modules.Dict)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        if (args[0] is Variable)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, args[0].Explain());
            return;
        }

        if (args[0] is Dict dict)
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
                var s1 = LanguageExtensions.Unify(args[1], key).TryGetValue(out var subs);
                if (s1)
                {
                    anyKey = true;
                    var s2 = LanguageExtensions.Unify(args[2], dict.Dictionary[key]).TryGetValue(out var vSubs);
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
                vm.Throw(ErgoVM.ErrorType.KeyNotFound, args[0].Explain(), args[1].Explain());
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
