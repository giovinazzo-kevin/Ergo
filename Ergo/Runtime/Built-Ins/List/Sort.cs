namespace Ergo.Runtime.BuiltIns;

public sealed class Sort : BuiltIn
{
    public Sort()
        : base("", new("sort"), 2, WellKnown.Modules.List)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args[0] is List list)
        {
            var sorted = new List(list.Contents.OrderBy(x => x), default, list.Scope);
            vm.SetArg(0, args[1]);
            vm.SetArg(1, sorted);
            ErgoVM.Goals.Unify2(vm);
        }
        else if (args[1] is Set set)
        {
            var lst = new List(set.Contents, default, set.Scope);
            vm.SetArg(0, args[0]);
            vm.SetArg(1, lst);
            ErgoVM.Goals.Unify2(vm);
        }
        else vm.Fail();
    };
}
