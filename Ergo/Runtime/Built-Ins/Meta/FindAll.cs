namespace Ergo.Runtime.BuiltIns;

public sealed class FindAll : BuiltIn
{
    public FindAll()
        : base("", new("findall"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args[1] is not NTuple comma)
        {
            comma = new([args[1]], default);
        }

        var newVm = vm.CreateChild();
        newVm.Query = newVm.CompileQuery(new(comma));
        newVm.Run();
        if (!newVm.Solutions.Any())
        {
            if (args[2].IsGround && args[2].Equals(WellKnown.Literals.EmptyList))
            {
                // noop
            }
            else if (!args[2].IsGround)
            {
                vm.SetArg(0, args[2]);
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
            var a0 = args[0];
            var list = new List(ImmutableArray.CreateRange(newVm.Solutions.Select(s => a0.Substitute(s.Substitutions))), default, default);
            if (args[2].IsGround && args[2].Equals(list))
            {
                // noop
            }
            else if (!args[2].IsGround)
            {
                vm.SetArg(0, args[2]);
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
