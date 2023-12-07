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
        var flags = scope.CompilerFlags;
        var hasFlag = flags.HasFlag(CompilerFlags.EnableOptimizations);
        if (hasFlag)
            flags &= ~CompilerFlags.EnableOptimizations;
        else
            flags |= CompilerFlags.EnableOptimizations;
        shell.WriteLine($"Compile-time optimizations {(!hasFlag ? "enabled" : "disabled")}.", LogLevel.Inf);
        yield return scope.WithCompilerFlags(flags);
    }
}
