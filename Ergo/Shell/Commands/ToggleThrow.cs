using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class ToggleThrow : ShellCommand
    {
        public ToggleThrow()
            : base(new[] { "throw" }, "", @"", 10)
        {
        }

        public override void Callback(ErgoShell s, Match m)
        {
            s.ThrowUnhandledExceptions = !s.ThrowUnhandledExceptions;
            s.WriteLine($"Throw mode {(s.ThrowUnhandledExceptions ? "enabled" : "disabled")}.", LogLevel.Inf);
        }
    }
}
