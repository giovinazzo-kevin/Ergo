using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Lang.ShellCommands
{
    public sealed class ToggleTrace : ShellCommand
    {
        public ToggleTrace()
            : base(new[] { "trace" }, "", @"", 10)
        {
        }

        public override void Callback(Shell s, Match m)
        {
            s.TraceMode = !s.TraceMode;
            s.WriteLine($"Trace mode {(s.TraceMode ? "enabled" : "disabled")}.", LogLevel.Inf);
        }
    }
}
