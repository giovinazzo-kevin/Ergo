using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class Load : ShellCommand
{
    public Load()
        : base(["load"], "Loads a module from file.", @"(?<path>.*)", true, 20)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        shell.Load(ref scope, m.Groups["path"].Value);
        yield return scope;
    }
}
