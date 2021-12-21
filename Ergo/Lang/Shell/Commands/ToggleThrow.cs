using System.Text.RegularExpressions;

namespace Ergo.Lang.ShellCommands
{
    public sealed class ToggleThrow : ShellCommand
    {
        public ToggleThrow()
            : base(new[] { "throw" }, "", @"", 10)
        {
        }

        public override void Callback(Shell s, Match m)
        {
            s.ThrowUnhandledExceptions = !s.ThrowUnhandledExceptions;
            s.WriteLine($"Throw mode {(s.ThrowUnhandledExceptions ? "enabled" : "disabled")}.", LogLevel.Inf);
        }
    }
}
