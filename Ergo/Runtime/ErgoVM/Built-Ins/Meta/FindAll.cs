namespace Ergo.Runtime.BuiltIns;

public sealed class FindAll : BuiltIn
{
    public FindAll()
        : base("", new("findall"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (vm.Arg(1) is not NTuple comma)
        {
            comma = new([vm.Arg(1)], default);
        }

        var newVm = vm.ScopedInstance();
        newVm.Query = newVm.CompileQuery(new(comma));
        newVm.Run();
        if (!newVm.Solutions.Any())
        {
            if (vm.Arg(2).IsGround && vm.Arg(2).Equals(WellKnown.Literals.EmptyList))
            {
                // noop
            }
            else if (!vm.Arg(2).IsGround)
            {
                vm.SetArg(0, vm.Arg(2));
                vm.SetArg(1, WellKnown.Literals.EmptyList);
                ErgoVM.Goals.Unify2(vm);
            }
            else
            {
                vm.Fail();
            }
        }
        else
        {
            var a0 = vm.Arg(0);
            var list = new List(ImmutableArray.CreateRange(newVm.Solutions.Select(s => a0.Substitute(s.Substitutions))), default, default);
            if (vm.Arg(2).IsGround && vm.Arg(2).Equals(list))
            {
                // noop
            }
            else if (!vm.Arg(2).IsGround)
            {
                vm.SetArg(0, vm.Arg(2));
                vm.SetArg(1, list);
                ErgoVM.Goals.Unify2(vm);
            }
            else
            {
                vm.Fail();
            }
        }
    };
}
