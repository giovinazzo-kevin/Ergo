using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public sealed class ToggleThrow : ShellCommand
    {
        public ToggleThrow()
            : base(new[] { "throw" }, "", @"", true, 20)
        {
        }

        public override async Task<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
        {
            scope = scope.WithExceptionThrowing(!scope.ExceptionThrowingEnabled);
            shell.WriteLine($"Throw mode {(scope.ExceptionThrowingEnabled ? "enabled" : "disabled")}.", LogLevel.Inf);
            return scope;
        }
    }
}
