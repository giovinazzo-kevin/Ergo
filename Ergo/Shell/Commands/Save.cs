using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class Save : ShellCommand
{
    public Save()
        : base(new[] { "save" }, "", @"(?<path>.*)", true, 20)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        shell.Save(scope, m.Groups["path"].Value);
        yield return scope;
    }
}
