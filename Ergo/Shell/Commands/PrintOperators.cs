using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class PrintOperators : ShellCommand
{
    public PrintOperators()
        : base(new[] { ":o", "operators" }, "Displays help about all operators that start with the given string", @"(?<op>[^\s].*)?", true, 70)
    {
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match match)
    {
        var operators = new List<Operator>();
        var startsWith = match.Groups["op"].Success ? match.Groups["op"].Value : "";
        foreach (var op in scope.InterpreterScope.VisibleOperators)
        {
            if (op.Synonyms.Any(s => s.Value.ToString().StartsWith(startsWith)))
            {
                operators.Add(op);
            }
        }

        var canonicals = operators
            .Select(r => new[] {
                r.Precedence.ToString(),
                Operator.GetOperatorType(r.Fixity, r.Associativity).ToString(),
                $"[{r.Synonyms.Join(x => x.AsQuoted(true).Explain(true))}]",
                r.DeclaringModule.Explain() })
            .ToArray();

        if (canonicals.Length == 0)
        {
            shell.No();
            yield return scope;
            yield break;
        }

        shell.WriteTable(new[] { "Precedence", "Affix", "Functors", "Module" }, canonicals, ConsoleColor.DarkYellow);
        yield return scope;
    }
}
