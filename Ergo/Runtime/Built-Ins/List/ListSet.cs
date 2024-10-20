namespace Ergo.Runtime.BuiltIns;

public sealed class ListSet : ErgoBuiltIn
{
    public ListSet()
        : base("", new("list_set"), 2, WellKnown.Modules.List)
    {
    }

    public override Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args[0] is List list)
        {
            var set = new Set(list.Contents, list.Scope);
            vm.SetArg(0, args[1]);
            vm.SetArg(1, set);
            ErgoVM.Goals.Unify2(vm);
        }
        else if (args[1] is Set set)
        {
            var lst = new List(set.Contents, default, set.Scope);
            vm.SetArg(0, args[0]);
            vm.SetArg(1, lst);
            ErgoVM.Goals.Unify2(vm);
        }
        else Ops.Fail(vm);
    };
}
