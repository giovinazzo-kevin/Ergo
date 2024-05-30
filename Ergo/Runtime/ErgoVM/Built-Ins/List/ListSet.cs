namespace Ergo.Runtime.BuiltIns;

public sealed class ListSet : BuiltIn
{
    public ListSet()
        : base("", "list_set", 2, WellKnown.Modules.List)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (vm.Memory.Dereference(vm.Args2[1]) is List list)
        {
            var set = new Set(list.Contents, list.Scope);
            vm.SetArg2(1, vm.Args2[2]);
            vm.SetArg2(2, vm.Memory.StoreTerm(set));
            ErgoVM.Goals.Unify2(vm);
        }
        else if (vm.Memory.Dereference(vm.Args2[2]) is Set set)
        {
            var lst = new List(set.Contents, default, set.Scope);
            vm.SetArg2(1, vm.Args2[1]);
            vm.SetArg2(2, vm.Memory.StoreTerm(lst));
            ErgoVM.Goals.Unify2(vm);
        }
        else ErgoVM.Ops.Fail(vm);
    };
}
