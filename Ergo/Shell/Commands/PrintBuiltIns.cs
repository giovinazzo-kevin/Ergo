using Ergo.Runtime.BuiltIns;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class PrintBuiltIns : ShellCommand
{
    public PrintBuiltIns()
        : base([":b", "builtins"], "Displays help about all built-ins that start with the given string", @"(?<term>[^\s].*)?", true, 80)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        await Task.CompletedTask;
        var match = m.Groups["term"];
        var builtins = new List<BuiltIn>();
        if (match?.Success ?? false)
        {
            var parsed = scope.InterpreterScope.Parse<ITerm>(match.Value);
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
            .Select(r => new[] { r.Signature.Explain(), r.Signature.Module.GetOr(default).Explain(), r.Documentation })
            .OrderBy(r => r[1])
            .ThenBy(r => r[0])
            .ToArray();

        if (canonicals.Length == 0)
        {
            shell.No();
            yield return scope;
            yield break;
        }

        shell.WriteTable(["Built-In", "Module", "Documentation"], canonicals, ConsoleColor.DarkRed);
        yield return scope;
    }
}
