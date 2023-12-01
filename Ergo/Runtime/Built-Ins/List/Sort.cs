
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Sort : BuiltIn
{
    public Sort()
        : base("", new("sort"), 2, WellKnown.Modules.List)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        if (args[0] is List list)
        {
            var sorted = new List(list.Contents.OrderBy(x => x), default, list.Scope);
            ErgoVM.Goals.Unify([args[1], sorted])(vm);
        }
        else if (args[1] is Set set)
        {
            var lst = new List(set.Contents, default, set.Scope);
            ErgoVM.Goals.Unify([args[0], lst])(vm);
        }
        else vm.Fail();
    };
}
