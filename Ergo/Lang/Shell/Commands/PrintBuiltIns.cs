using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Lang.ShellCommands
{
    public sealed class PrintBuiltIns : ShellCommand
    {
        public PrintBuiltIns()
            : base(new[] { ":#", "builtin" }, "Displays help about all commands that start with the given string", @"(?<term>[^\s].*)?", 100)
        {
        }

        public override void Callback(Shell s, Match m)
        {
            var match = m.Groups["term"];
            var builtins = new List<BuiltIn>();
            if (match?.Success ?? false)
            {
                var parsed = s.Parse<ITerm>(match.Value).Value;
                if (!parsed.HasValue)
                {
                    s.No();
                    return;
                }
                var term = parsed.Reduce(some => some, () => default);
                if (s.Interpreter.TryGetBuiltIn(term, out var builtin))
                {
                    builtins.Add(builtin);
                }
                else
                {
                    s.No();
                    return;
                }
            }
            else
            {
                builtins.AddRange(s.Interpreter.BuiltIns);
            }

            var canonicals = builtins
                .Select(r => new[] { r.Signature.Explain(), r.Documentation })
                .ToArray();

            if (canonicals.Length == 0)
            {
                s.No();
                return;
            }

            s.WriteTable(new[] { "Built-In", "Documentation" }, canonicals, ConsoleColor.DarkRed);
        }
    }
}
