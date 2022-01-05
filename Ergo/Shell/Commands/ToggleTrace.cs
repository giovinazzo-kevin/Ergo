using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class ToggleTrace : ShellCommand
    {
        public ToggleTrace()
            : base(new[] { "trace" }, "", @"", true, 20)
        {
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            scope = scope.WithTrace(!scope.TraceEnabled);
            shell.WriteLine($"Trace mode {(scope.TraceEnabled ? "enabled" : "disabled")}.", LogLevel.Inf);
        }
    }
}
