using Ergo.Solver;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class ToggleCompiler : ShellCommand
{
    public ToggleCompiler()
        : base(new[] { "#compiler" }, "Enables/disables the compiler.", @"", true, 21)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var flags = scope.SolverFlags;
        var hasFlag = flags.HasFlag(SolverFlags.EnableCompiler);
        if (hasFlag)
            flags &= ~SolverFlags.EnableCompiler;
        else
            flags |= SolverFlags.EnableCompiler;
        shell.WriteLine($"Compiler {(!hasFlag ? "enabled" : "disabled")}.", LogLevel.Inf);
        yield return scope.WithSolverFlags(flags);
    }
}
