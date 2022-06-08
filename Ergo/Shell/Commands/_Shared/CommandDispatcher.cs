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

        public async Task<Maybe<ShellScope>> Dispatch(ErgoShell shell, ShellScope scope, string input)
        {
            foreach (var d in Commands.OrderByDescending(c => c.Priority))
            {
                if (d.Expression.Match(input) is { Success: true } match)
                {
                    scope = await d.Callback(shell, scope, match);
                    return Maybe.Some(scope);
                }
            }
            DefaultDispatcher(input);
            return Maybe<ShellScope>.None;
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
