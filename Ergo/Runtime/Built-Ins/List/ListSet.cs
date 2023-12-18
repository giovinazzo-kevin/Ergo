namespace Ergo.Runtime.BuiltIns;

public sealed class ListSet : BuiltIn
{
    public ListSet()
        : base("", new("list_set"), 2, WellKnown.Modules.List)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
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
            vm.SetArg(0, args[1]);
            vm.SetArg(1, lst);
            ErgoVM.Goals.Unify2(vm);
        }
        else ErgoVM.Ops.Fail(vm);
    };
}
