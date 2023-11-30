using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class IsSet : SolverBuiltIn
{
    public IsSet()
        : base("", new("is_set"), 1, WellKnown.Modules.Set)
    {

    }

    public override ErgoVM.Goal Compile() => args => args[0] is Set ? ErgoVM.Ops.NoOp : ErgoVM.Ops.Fail;
}
