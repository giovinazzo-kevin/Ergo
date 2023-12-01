
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class FindAll : BuiltIn
{
    public FindAll()
        : base("", new("findall"), 3, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
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
                ErgoVM.Goals.Unify([args[2], WellKnown.Literals.EmptyList])(vm);
            }
            else
            {
                vm.Fail();
            }
        }
        else
        {
            var list = new List(ImmutableArray.CreateRange(newVm.Solutions.Select(s => args[0].Substitute(s.Substitutions))), default, default);
            if (args[2].IsGround && args[2].Equals(list))
            {
                // noop
            }
            else if (!args[2].IsGround)
            {
                ErgoVM.Goals.Unify([args[2], list])(vm);
            }
            else
            {
                vm.Fail();
            }
        }
    };
}
