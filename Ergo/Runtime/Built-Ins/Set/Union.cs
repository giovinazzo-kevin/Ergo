namespace Ergo.Runtime.BuiltIns;

public sealed class Union : ErgoBuiltIn
{
    public Union()
        : base("", new("union"), 3, WellKnown.Modules.Set)
    {

    }

    public override Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args[0] is Set s1)
        {
            if (args[1] is Set s2)
            {
                var s3 = new Set(s1.Contents.Union(s2.Contents), s1.Scope);
                vm.SetArg(0, args[2]);
                vm.SetArg(1, s3);
                ErgoVM.Goals.Unify2(vm);
            }
            else if (args[2] is Set s3)
            {
                s2 = new Set(s3.Contents.Except(s1.Contents), s3.Scope);
                vm.SetArg(0, args[1]);
                vm.SetArg(1, s2);
                ErgoVM.Goals.Unify2(vm);
            }
        }
        else if (args[1] is Set s2 && args[2] is Set s3)
        {
            s1 = new Set(s3.Contents.Except(s2.Contents), s3.Scope);
            vm.SetArg(0, args[0]);
            vm.SetArg(1, s1);
            ErgoVM.Goals.Unify2(vm);
        }
        else vm.Fail();
    };
}
