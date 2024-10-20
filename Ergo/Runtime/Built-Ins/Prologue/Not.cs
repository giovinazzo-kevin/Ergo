﻿
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Not : ErgoBuiltIn
{
    public Not()
        : base("", new("not"), Maybe<int>.Some(1), WellKnown.Modules.Prologue)
    {
    }

    public override ExecutionNode Optimize(BuiltInNode node)
    {
        return node; // TODO: fix bug with \+(a </> _)
        var op = node.Goal.GetArguments()[0]
            .ToExecutionNode(node.Node.Graph, ctx: new("NOT"))
            .Optimize()
            .Compile();
        return new VirtualNode(vm =>
        {
            var newVm = vm.ScopedInstance();
            newVm.Query = op;
            newVm.Run();
            if (newVm.Solutions.Any())
                vm.Fail();
        }, node.Goal.GetArguments()[0].GetArguments());
    }

    public override Op Compile() => vm =>
    {
        // NOTE: This will never be called if optimizations are enabled.
        // Which is good, because compiling the query on the fly is expensive.
        var newVm = vm.ScopedInstance();
        newVm.Query = vm.CompileQuery(new(vm.Arg(0)));
        newVm.Run();
        if (newVm.Solutions.Any())
            vm.Fail();
    };
}
