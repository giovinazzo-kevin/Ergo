using Ergo.Solver;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class ToggleOptimizations : ShellCommand
{
    public ToggleOptimizations()
        : base(new[] { "#optimizations" }, "Enables/disables compile-time optimizations.", @"", true, 22)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var flags = scope.SolverFlags;
        var hasFlag = flags.HasFlag(SolverFlags.EnableCompilerOptimizations);
        if (hasFlag)
            flags &= ~SolverFlags.EnableCompilerOptimizations;
        else
            flags |= SolverFlags.EnableCompilerOptimizations;
        shell.WriteLine($"Compile-time optimizations {(!hasFlag ? "enabled" : "disabled")}.", LogLevel.Inf);
        yield return scope.WithSolverFlags(flags);
    }
}
