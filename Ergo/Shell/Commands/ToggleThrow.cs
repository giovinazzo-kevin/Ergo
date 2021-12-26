using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class ToggleThrow : ShellCommand
    {
        public ToggleThrow()
            : base(new[] { "throw" }, "", @"", 20)
        {
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            scope = scope.WithExceptionThrowing(!scope.ExceptionThrowingEnabled);
            shell.WriteLine($"Throw mode {(scope.ExceptionThrowingEnabled ? "enabled" : "disabled")}.", LogLevel.Inf);
        }
    }
}
