using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Cut : SolverBuiltIn
{
    public Cut()
        : base("", new("!"), Maybe<int>.Some(0), WellKnown.Modules.Prologue)
    {
    }

    public override ErgoVM.Goal Compile() => args => ErgoVM.Ops.Cut;
}
