using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Nonvar : SolverBuiltIn
{
    public Nonvar()
        : base("", new("nonvar"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Goal Compile() => args => args[0] is not Variable ? ErgoVM.Ops.NoOp : ErgoVM.Ops.Fail;
}
