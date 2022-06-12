using Ergo.Solver;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public sealed class PrintDataSinks : ShellCommand
{
    public PrintDataSinks()
        : base(new[] { ":<", "sinks" }, "Displays help about all data sinks that start with the given string", @"(?<term>[^\s].*)?", true, 60)
    {

    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var match = m.Groups["term"];
        var sinks = new List<Signature>();
        var solver = new ErgoSolver(default, default, default);
        shell.ConfigureSolver(solver);
        var parsed = shell.Parse<ITerm>(scope, match.Success ? match.Value : "_").Value;
        if (!parsed.HasValue)
        {
            shell.No();
            yield return scope;
            yield break;
        }

        var term = parsed.GetOrDefault();
        var signature = term.GetSignature().WithModule(Maybe.None<Atom>());
        foreach (var functor in solver.DataSinks)
        {
            if (!term.IsGround || new Substitution(signature.Functor, functor).Unify().HasValue)
            {
                sinks.Add(new(functor, Maybe<int>.None, Maybe<Atom>.None, Maybe<Atom>.None));
            }
        }

        var canonicals = sinks
            .Select(r => new[] { r.Explain() })
            .ToArray();

        if (canonicals.Length == 0)
        {
            shell.No();
            yield return scope;
            yield break;
        }

        shell.WriteTable(new[] { "Data sink" }, canonicals, ConsoleColor.DarkGray);
        yield return scope;
    }
}
