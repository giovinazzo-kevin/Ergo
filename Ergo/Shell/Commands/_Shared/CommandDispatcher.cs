using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;

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

        public bool Dispatch(ErgoShell shell, ref ShellScope scope, string input)
        {
            foreach (var d in Commands.OrderByDescending(c => c.Priority))
            {
                if (d.Expression.Match(input) is { Success: true } match)
                {
                    d.Callback(shell, ref scope, match);
                    return true;
                }
            }
            DefaultDispatcher(input);
            return false;
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
