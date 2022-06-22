using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class ToggleThrow : ShellCommand
{
    public ToggleThrow()
        : base(new[] { "throw" }, "", @"", true, 20)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var throwingEnabled = scope.InterpreterScope.ExceptionHandler.Equals(shell.ThrowingExceptionHandler);
        var handler = throwingEnabled
            ? shell.LoggingExceptionHandler : shell.ThrowingExceptionHandler;

        scope = scope.WithInterpreterScope(scope.InterpreterScope.WithExceptionHandler(handler));
        shell.WriteLine($"Throw mode {(!throwingEnabled ? "enabled" : "disabled")}.", LogLevel.Inf);
        yield return scope;
    }
}
