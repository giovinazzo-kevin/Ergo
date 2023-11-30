using Ergo.Lang.Compiler;
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
        var flags = scope.VMFlags;
        var hasFlag = flags.HasFlag(VMFlags.EnableCompiler);
        if (hasFlag)
            flags &= ~VMFlags.EnableCompiler;
        else
            flags |= VMFlags.EnableCompiler;
        shell.WriteLine($"Compiler {(!hasFlag ? "enabled" : "disabled")}.", LogLevel.Inf);
        yield return scope.WithVMFlags(flags);
    }
}
