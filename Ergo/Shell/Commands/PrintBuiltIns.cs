using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;
using Ergo.Solver.BuiltIns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public sealed class PrintBuiltIns : ShellCommand
    {
        public PrintBuiltIns()
            : base(new[] { ":b", "builtins" }, "Displays help about all built-ins that start with the given string", @"(?<term>[^\s].*)?", true, 80)
        {
        }

        public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
        {
            var match = m.Groups["term"];
            var builtins = new List<BuiltIn>();
            using var solver = SolverBuilder.Build(shell.Interpreter, ref scope);
            if (match?.Success ?? false)
            {
                var parsed = shell.Parse<ITerm>(scope, match.Value).Value;
                if (!parsed.HasValue)
                {
                    shell.No();
                    yield return scope;
                    yield break;
                }
                var term = parsed.GetOrDefault();
                if (solver.BuiltIns.TryGetValue(term.GetSignature(), out var builtin))
                {
                    builtins.Add(builtin);
                }
                else
                {
                    shell.No();
                    yield return scope;
                    yield break;
                }
            }
            else
            {
                builtins.AddRange(solver.BuiltIns.Values);
            }

            var canonicals = builtins
                .Select(r => new[] { r.Signature.Explain(), r.Documentation })
                .ToArray();

            if (canonicals.Length == 0)
            {
                shell.No();
                yield return scope;
                yield break;
            }

            shell.WriteTable(new[] { "Built-In", "Documentation" }, canonicals, ConsoleColor.DarkRed);
            yield return scope;
        }
    }
}
