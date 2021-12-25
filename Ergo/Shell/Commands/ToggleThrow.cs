using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class ToggleThrow : ShellCommand
    {
        public ToggleThrow()
            : base(new[] { "throw" }, "", @"", 10)
        {
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            shell.ThrowUnhandledExceptions = !shell.ThrowUnhandledExceptions;
            shell.WriteLine($"Throw mode {(shell.ThrowUnhandledExceptions ? "enabled" : "disabled")}.", LogLevel.Inf);
        }
    }
}
