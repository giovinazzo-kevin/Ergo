﻿using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Ground : SolverBuiltIn
{
    public Ground()
        : base("", new("ground"), Maybe<int>.Some(1), WellKnown.Modules.Reflection)
    {
    }

    public override ExecutionNode Optimize(BuiltInNode node) =>
        node.Goal.IsGround ? TrueNode.Instance : FalseNode.Instance;
    public override ErgoVM.Goal Compile() => args => args[0].IsGround ? ErgoVM.Ops.NoOp : ErgoVM.Ops.Fail;
}
