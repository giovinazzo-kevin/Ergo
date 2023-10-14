using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class ClearScreen : ShellCommand
{
    public ClearScreen()
        : base(new[] { "cls" }, "Clears the screen.", @"", true, 1000)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        shell.Clear();
        yield return scope;
    }
}
