using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class CopyTerm : SolverBuiltIn
{
    public CopyTerm()
        : base("", new("copy_term"), Maybe<int>.Some(2), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Goal Compile() => args => vm =>
    {
        var copy = args[0].Instantiate(vm.InstantiationContext);
        ErgoVM.Goals.Unify(args.SetItem(0, copy))(vm);
    };
}
