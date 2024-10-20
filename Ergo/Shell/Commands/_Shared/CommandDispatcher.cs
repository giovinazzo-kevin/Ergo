using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Ergo.Shell.Commands;

public partial class CommandDispatcher
{
    protected readonly List<ShellCommand> CommandList;
    protected readonly Action<string> DefaultDispatcher;

    public IEnumerable<ShellCommand> Commands => CommandList;

    public CommandDispatcher([NotNull] Action<string> unknownCommand)
    {
        Contract.Requires(unknownCommand is { });
        CommandList = [];
        DefaultDispatcher = unknownCommand;
    }

    public async IAsyncEnumerable<ShellScope> Dispatch(ErgoShell shell, ShellScope scope, string input)
    {
        foreach (var d in Commands.OrderByDescending(c => c.Priority))
        {
            if (d.Expression.Match(input) is { Success: true } match)
            {
                await foreach (var newScope in d.Callback(shell, scope, match))
                {
                    yield return newScope;
                }

                yield break;
            }
        }

        DefaultDispatcher(input);
    }

    public void Add(ShellCommand cmd)
    {
        if (Commands.Any(d => d.Names.Intersect(cmd.Names).Any()))
        {
            throw new NotSupportedException($"A shell command with one of these names is already defined: {cmd.Names.Join()}");
        }

        CommandList.Add(cmd);
    }

    public bool Remove(string name) => CommandList.RemoveAll(d => d.Names.Contains(name)) > 0;
}
