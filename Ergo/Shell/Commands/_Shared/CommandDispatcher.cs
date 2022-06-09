using Ergo.Lang;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public partial class CommandDispatcher
    {
        protected readonly List<ShellCommand> CommandList;
        protected readonly Action<string> DefaultDispatcher;

        public IEnumerable<ShellCommand> Commands => CommandList;

        public CommandDispatcher([NotNull] Action<string> unknownCommand)
        {
            Contract.Requires(unknownCommand is { });
            CommandList = new List<ShellCommand>();
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

        public bool TryAdd(ShellCommand cmd)
        {
            if (Commands.Any(d => d.Names.Intersect(cmd.Names).Any()))
            {
                return false;
            }

            CommandList.Add(cmd);
            return true;
        }

        public bool Remove(string name)
        {
            return CommandList.RemoveAll(d => d.Names.Contains(name)) > 0;
        }
    }
}
