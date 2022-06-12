using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class ToggleTrace : ShellCommand
{
    public ToggleTrace()
        : base(new[] { "trace" }, "", @"", true, 20)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        scope = scope.WithTrace(!scope.TraceEnabled);
        shell.WriteLine($"Trace mode {(scope.TraceEnabled ? "enabled" : "disabled")}.", LogLevel.Inf);
        yield return scope;
    }
}
