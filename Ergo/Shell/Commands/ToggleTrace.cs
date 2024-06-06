using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class ToggleTrace : ShellCommand
{
    public ToggleTrace()
        : base(["trace"], "Enables/disables the interactive trace.", @"", true, 20)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        await Task.CompletedTask;
        scope = scope.WithTrace(!scope.TraceEnabled);
        shell.WriteLine($"Trace mode {(scope.TraceEnabled ? "enabled" : "disabled")}.", LogLevel.Inf);
        if (scope.TraceEnabled)
        {
            shell.WriteLine("While in trace mode, press:", LogLevel.Cmt);
            shell.WriteLine("\t- spacebar to creep through;", LogLevel.Cmt);
            shell.WriteLine("\t- 'c' to continue until the next solution;", LogLevel.Cmt);
            shell.WriteLine("\t- any other key to abort.", LogLevel.Cmt);
        }

        yield return scope;
    }
}
