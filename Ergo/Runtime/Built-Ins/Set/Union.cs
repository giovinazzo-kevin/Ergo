using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Union : BuiltIn
{
    public Union()
        : base("", new("union"), 3, WellKnown.Modules.Set)
    {

    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        if (args[0] is Set s1)
        {
            if (args[1] is Set s2)
            {
                var s3 = new Set(s1.Contents.Union(s2.Contents), s1.Scope);
                ErgoVM.Goals.Unify([args[2], s3])(vm);
            }
            else if (args[2] is Set s3)
            {
                s2 = new Set(s3.Contents.Except(s1.Contents), s3.Scope);
                ErgoVM.Goals.Unify([args[1], s2])(vm);
            }
        }
        else if (args[1] is Set s2 && args[2] is Set s3)
        {
            s1 = new Set(s3.Contents.Except(s2.Contents), s3.Scope);
            ErgoVM.Goals.Unify([args[0], s1])(vm);
        }
        else vm.Fail();
    };
}
