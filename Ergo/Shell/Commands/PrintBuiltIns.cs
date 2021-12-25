using Ergo.Lang.Ast;
using Ergo.Solver.BuiltIns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    //public sealed class PrintBuiltIns : ShellCommand
    //{
    //    public PrintBuiltIns()
    //        : base(new[] { ":#", "builtin" }, "Displays help about all commands that start with the given string", @"(?<term>[^\s].*)?", 100)
    //    {
    //    }

    //    public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
    //    {
    //        var match = m.Groups["term"];
    //        var builtins = new List<BuiltIn>();
    //        if (match?.Success ?? false)
    //        {
    //            var parsed = shell.Parse<ITerm>(scope, match.Value).Value;
    //            if (!parsed.HasValue)
    //            {
    //                shell.No();
    //                return;
    //            }
    //            var term = parsed.Reduce(some => some, () => default);
    //            if (shell.Interpreter.TryGetBuiltIn(term, out var builtin))
    //            {
    //                builtins.Add(builtin);
    //            }
    //            else
    //            {
    //                shell.No();
    //                return;
    //            }
    //        }
    //        else
    //        {
    //            builtins.AddRange(shell.Interpreter.BuiltIns);
    //        }

    //        var canonicals = builtins
    //            .Select(r => new[] { r.Signature.Explain(), r.Documentation })
    //            .ToArray();

    //        if (canonicals.Length == 0)
    //        {
    //            shell.No();
    //            return;
    //        }

    //        shell.WriteTable(new[] { "Built-In", "Documentation" }, canonicals, ConsoleColor.DarkRed);
    //    }
    //}
}
