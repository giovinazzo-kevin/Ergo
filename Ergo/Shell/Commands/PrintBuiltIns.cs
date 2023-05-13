using Ergo.Solver.BuiltIns;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class PrintBuiltIns : ShellCommand
{
    public PrintBuiltIns()
        : base(new[] { ":b", "builtins" }, "Displays help about all built-ins that start with the given string", @"(?<term>[^\s].*)?", true, 80)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var match = m.Groups["term"];
        var builtins = new List<SolverBuiltIn>();
        using var solver = shell.Facade.BuildSolver();
        if (match?.Success ?? false)
        {
            var parsed = shell.Interpreter.Facade.Parse<ITerm>(scope.InterpreterScope, match.Value);
            if (!parsed.TryGetValue(out var term))
            {
                shell.No();
                yield return scope;
                yield break;
            }

            if (scope.InterpreterScope.VisibleBuiltIns.TryGetValue(term.GetSignature(), out var builtin))
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
            builtins.AddRange(scope.InterpreterScope.VisibleBuiltIns.Values);
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
