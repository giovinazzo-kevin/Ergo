using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class ToggleTrace : ShellCommand
    {
        public ToggleTrace()
            : base(new[] { "trace" }, "", @"", 10)
        {
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            shell.TraceMode = !shell.TraceMode;
            shell.WriteLine($"Trace mode {(shell.TraceMode ? "enabled" : "disabled")}.", LogLevel.Inf);
        }
    }
}
