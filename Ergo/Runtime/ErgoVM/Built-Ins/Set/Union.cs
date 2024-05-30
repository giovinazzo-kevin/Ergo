namespace Ergo.Runtime.BuiltIns;

public sealed class Union : BuiltIn
{
    public Union()
        : base("", "union", 3, WellKnown.Modules.Set)
    {

    }

    public override ErgoVM.Op Compile() => vm =>
    {
        if (vm.Arg(0) is Set s1)
        {
            if (vm.Arg(1) is Set s2)
            {
                var s3 = new Set(s1.Contents.Union(s2.Contents), s1.Scope);
                vm.SetArg(0, vm.Arg(2));
                vm.SetArg(1, s3);
                ErgoVM.Goals.Unify2(vm);
            }
            else if (vm.Arg(2) is Set s3)
            {
                s2 = new Set(s3.Contents.Except(s1.Contents), s3.Scope);
                vm.SetArg(0, vm.Arg(1));
                vm.SetArg(1, s2);
                ErgoVM.Goals.Unify2(vm);
            }
        }
        else if (vm.Arg(1) is Set s2 && vm.Arg(2) is Set s3)
        {
            s1 = new Set(s3.Contents.Except(s2.Contents), s3.Scope);
            vm.SetArg(0, vm.Arg(0));
            vm.SetArg(1, s1);
            ErgoVM.Goals.Unify2(vm);
        }
        else vm.Fail();
    };
}
