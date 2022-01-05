using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public sealed class PrintHelp : ShellCommand
    {
        public PrintHelp()
            : base(new[] { "?", "help" }, "Displays help about all commands that start with the given string", @"(?<cmd>[^\s].*)?", true, 100)
        {
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
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
                .Select(d => new[] { string.Join(", ", d.Names), d.Priority.ToString(), d.Description })
                .ToArray();
            if (dispatchers.Length == 0)
            {
                shell.Dispatcher.Dispatch(shell, ref scope, cmd.Value); // Dispatches UnknownCommand
                return;
            }
            shell.WriteTable(new[] { "Command", "Priority", "Description" }, dispatchers, ConsoleColor.DarkGreen);
        }
    }
}
