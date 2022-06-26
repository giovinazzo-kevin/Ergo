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
        if (scope.TraceEnabled)
        {
            shell.WriteLine("While in trace mode, press:" +
                "\r\n\t- spacebar to creep through;" +
                "\r\n\t- 'c' to continue until the next solution;" +
                "\r\n\t- any other key to abort.", LogLevel.Cmt);
        }

        yield return scope;
    }
}
