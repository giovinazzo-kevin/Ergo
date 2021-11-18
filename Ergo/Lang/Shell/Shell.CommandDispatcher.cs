using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{
    public partial class Shell
    {
        public partial class CommandDispatcher
        {
            protected readonly List<Dispatcher> Dispatchers;
            protected readonly Action<string> DefaultDispatcher;

            public IEnumerable<Dispatcher> RegisteredDispatchers => Dispatchers;

            public CommandDispatcher([NotNull] Action<string> unknownCommand)
            {
                Contract.Requires(unknownCommand is { });
                Dispatchers = new List<Dispatcher>();
                DefaultDispatcher = unknownCommand;
            }

            public bool Dispatch(string input)
            {
                foreach (var d in Dispatchers) {
                    if(d.Expression.Match(input) is { Success: true } match) {
                        d.Callback(match);
                        return true;
                    }
                }
                DefaultDispatcher(input);
                return false;
            }

            public void Add(string exp, Action<Match> callback, string name, string desc, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled)
            {
                if(Dispatchers.Any(d => d.Name == name)) {
                    throw new ArgumentException(nameof(name));
                }

                Dispatchers.Add(new Dispatcher(name, desc, new Regex(exp, options), callback));
            }

            public bool Remove(string name)
            {
                return Dispatchers.RemoveAll(d => d.Name == name) > 0;
            }
        }
    }
}
