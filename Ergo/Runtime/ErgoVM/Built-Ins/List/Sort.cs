namespace Ergo.Runtime.BuiltIns;

public sealed class Sort : BuiltIn
{
    public Sort()
        : base("", "sort", 2, WellKnown.Modules.List)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args2;
        if (vm.Memory.Dereference(args[1]) is List list)
        {
            var sorted = new List(list.Contents.OrderBy(x => x), default, list.Scope);
            vm.SetArg2(2, vm.Memory.StoreTerm(sorted));
            ErgoVM.Goals.Unify2(vm);
        }
        else if (vm.Memory.Dereference(args[2]) is Set set)
        {
            var lst = new List(set.Contents, default, set.Scope);
            vm.SetArg2(2, vm.Memory.StoreTerm(lst));
            ErgoVM.Goals.Unify2(vm);
        }
        else vm.Fail();
    };
}
