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
        var args = vm.Args2;
        var allSubs = SubstitutionMap.Pool.Acquire();
        var (start, end) = (0, 0);
        var arg0 = vm.Arg(0);
        var arg1 = vm.Arg(1);
        if (vm.Memory.Unify(args[2], vm.Memory.StoreAtom(new Atom(start))))
        {

        }
        else if (arg1 is Atom { Value: EDecimal d })
        {
            start = d.ToInt32IfExact();
        }
        else if (arg1 is not Atom)
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[1]);
            return;
        }
        var newVars = new Dictionary<string, Variable>();
        foreach (var (v, i) in arg0.Variables.Select((v, i) => (v, i)))
        {
            newVars[v.Name] = new Variable($"$VAR({i})");
            ++end;
        }
        if (!vm.Memory.Unify(vm.Memory.StoreTerm(arg0.Instantiate(vm.InstantiationContext, newVars)), args[1]))
        {
            vm.Fail();
            return;
        }
        var arg2 = vm.Arg(2);
        if (vm.Memory.Unify(args[3], vm.Memory.StoreAtom(new Atom(end))))
        {

        }
        else if (arg2 is Atom { Value: EDecimal d } && d.ToInt32IfExact() != end)
        {
            vm.Fail();
            return;
        }
        else if (arg1 is not Atom)
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Number, args[1]);
            return;
        }
        vm.Solution(allSubs);
    };
}
