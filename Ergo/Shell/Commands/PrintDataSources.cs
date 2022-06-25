using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class PrintDataSources : ShellCommand
{
    public PrintDataSources()
        : base(new[] { ":>", "sources" }, "Displays help about all data sources that start with the given string", @"(?<term>[^\s].*)?", true, 60)
    {

    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var match = m.Groups["term"];
        var sources = new List<Signature>();
        var solver = shell.Facade.BuildSolver();
        var parsed = shell.Parse<ITerm>(scope, match.Success ? match.Value : "_").Value;
        if (!parsed.HasValue)
        {
            shell.No();
            yield return scope;
            yield break;
        }

        var term = parsed.GetOrDefault();
        var signature = term.GetSignature().WithModule(Maybe.None<Atom>());
        if (solver.DataSources.TryGetValue(signature, out _))
        {
            sources.Add(signature);
        }
        else if (term is Variable)
        {
            sources.AddRange(solver.DataSources.Keys);
        }

        var canonicals = sources
            .Select(r => new[] { r.Explain() })
            .ToArray();

        if (canonicals.Length == 0)
        {
            shell.No();
            yield return scope;
            yield break;
        }

        shell.WriteTable(new[] { "Data source" }, canonicals, ConsoleColor.DarkGray);
        yield return scope;
    }
}
