
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class ListSet : BuiltIn
{
    public ListSet()
        : base("", new("list_set"), 2, WellKnown.Modules.List)
    {
    }

    public override ErgoVM.Goal Compile() => args =>
    {
        if (args[0] is List list)
        {
            var set = new Set(list.Contents, list.Scope);
            return ErgoVM.Goals.Unify([args[1], set]);
        }
        else if (args[1] is Set set)
        {
            var lst = new List(set.Contents, default, set.Scope);
            return ErgoVM.Goals.Unify([args[1], lst]);
        }
        return ErgoVM.Ops.Fail;
    };
}
