using PeterO.Numbers;

namespace Ergo.Runtime.BuiltIns;

public sealed class NumberVars : BuiltIn
{
    public NumberVars()
        : base("", new("numbervars"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        var allSubs = SubstitutionMap.Pool.Acquire();
        var (start, end) = (0, 0);
        if (LanguageExtensions.Unify(args[1], new Atom(start)).TryGetValue(out var subs1))
        {
            if (!args[1].IsGround)
            {
                allSubs.AddRange(subs1);
            }
            SubstitutionMap.Pool.Release(subs1);
        }
        else if (args[1].IsGround && args[1] is Atom { Value: EDecimal d })
        {
            start = d.ToInt32IfExact();
        }
        else if (args[1] is not Atom)
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[1]);
            return;
        }
        var newVars = new Dictionary<string, Variable>();
        foreach (var (v, i) in args[0].Variables.Select((v, i) => (v, i)))
        {
            newVars[v.Name] = new Variable($"$VAR({i})");
            ++end;
        }
        if (!LanguageExtensions.Unify(args[0].Instantiate(vm.InstantiationContext, newVars), args[0]).TryGetValue(out var subs0))
        {
            vm.Fail();
        }
        allSubs.AddRange(subs0);
        SubstitutionMap.Pool.Release(subs0);
        if (LanguageExtensions.Unify(args[2], new Atom(end)).TryGetValue(out var subs2))
        {
            if (!args[2].IsGround)
            {
                allSubs.AddRange(subs2);
            }
            SubstitutionMap.Pool.Release(subs2);
        }
        else if (args[2].IsGround && args[2] is Atom { Value: EDecimal d } && d.ToInt32IfExact() != end)
        {
            vm.Fail();
        }
        else if (args[1] is not Atom)
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[1]);
            return;
        }
        vm.Solution(allSubs);
    };
}
