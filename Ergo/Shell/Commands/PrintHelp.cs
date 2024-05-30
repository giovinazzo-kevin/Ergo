using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class PrintHelp : ShellCommand
{
    public PrintHelp()
        : base(["?", "help"], "Displays help about all commands that start with the given string", @"(?<cmd>[^\s].*)?", true, 100)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var cmd = m.Groups["cmd"];
        var dispatchersQuery = shell.Dispatcher.Commands;
        if (cmd?.Success ?? false)
        {
            var alias = $"alias: {cmd.Value}";
            dispatchersQuery = dispatchersQuery.Where(d =>
                d.Names.Any(n => n.StartsWith(cmd.Value, StringComparison.OrdinalIgnoreCase) || n.Contains(alias, StringComparison.OrdinalIgnoreCase))
            );
        }

        var dispatchers = dispatchersQuery
            .OrderByDescending(d => d.Priority)
            .Select(d => new[] { d.Names.Join(), d.Priority.ToString(), d.Description })
            .ToArray();
        if (dispatchers.Length == 0)
        {
            await foreach (var result in shell.Dispatcher.Dispatch(shell, scope, cmd.Value))
            {
                yield return result;
            }
        }

        shell.WriteTable(["Command", "Priority", "Description"], dispatchers, ConsoleColor.DarkGreen);
        yield return scope;
    }
}
