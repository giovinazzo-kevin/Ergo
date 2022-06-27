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
        using var solver = shell.Facade.BuildSolver();
        var parsed = shell.Interpreter.Parse<ITerm>(scope.InterpreterScope, match.Success ? match.Value : "_");
        if (!parsed.TryGetValue(out var term))
        {
            shell.No();
            yield return scope;
            yield break;
        }

        var signature = term.GetSignature().WithModule(Maybe.None<Atom>());
        foreach (var functor in solver.DataSinks)
        {
            if (!term.IsGround || new Substitution(signature.Functor, functor).Unify().TryGetValue(out _))
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
