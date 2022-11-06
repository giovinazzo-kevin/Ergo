using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public abstract class SolveShellCommand : ShellCommand
{
    public readonly bool Interactive;
    public readonly ConsoleColor DefaultAccentColor;

    static string GetColorAlternatives()
    {
        var colorMap = Enum.GetNames(typeof(ConsoleColor))
            .Select(v => v.ToString())
            .ToList();
        return colorMap.Join("|");
    }

    protected SolveShellCommand(string[] names, string desc, int priority, bool interactive, ConsoleColor accentColor)
        : base(names, desc, $@"(?<color>{GetColorAlternatives()})?\s*(?<query>(?:[^\s].*\s*=)?\s*[^\s].*)", true, priority, caseInsensitive: true)
    {
        Interactive = interactive;
        DefaultAccentColor = accentColor;
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var executionAbortedCtrlC = false;
        var requestCancel = new CancellationTokenSource();
        Console.CancelKeyPress += RequestCancel;
        var userQuery = m.Groups["query"].Value;
        var accent = m.Groups["color"] is { Success: true, Value: var v }
            ? Enum.Parse<ConsoleColor>(v, true)
            : DefaultAccentColor;
        if (!userQuery.EndsWith('.'))
        {
            // Syntactic sugar
            userQuery += '.';
        }

        using var solver = shell.Facade.BuildSolver(scope.InterpreterScope.KnowledgeBase);
        var parsed = shell.Interpreter.Parse<Query>(scope.InterpreterScope, userQuery);
        if (!parsed.TryGetValue(out var query))
        {
            yield return scope;
            yield break;
        }

        shell.WriteLine(query.Goals.Explain(), LogLevel.Dbg);
        var (nonInteractiveTrace, nonInteractiveSolve) = (false, false);
        if (scope.TraceEnabled)
        {
            solver.Trace += (type, trace) =>
            {
                shell.Write(trace, LogLevel.Trc, type);
                shell.Write("? ", LogLevel.Rpl, overrideFg: ConsoleColor.DarkMagenta);
                switch (char.ToLower(nonInteractiveTrace ? ' ' : shell.ReadChar(true)))
                {
                    case 'c':
                        shell.Write("continue", LogLevel.Rpl, overrideFg: ConsoleColor.Magenta);
                        nonInteractiveTrace = true;
                        break;
                    case ' ':
                        shell.Write("creep", LogLevel.Rpl, overrideFg: ConsoleColor.DarkMagenta);
                        break;
                    default:
                        shell.Write("abort", LogLevel.Rpl, overrideFg: ConsoleColor.Red);
                        requestCancel.Cancel();
                        break;
                }

                shell.WriteLine();
            };
        }

        var solutions = solver.SolveAsync(query, solver.CreateScope(scope.InterpreterScope), ct: requestCancel.Token); // Solution graph is walked lazily
        if (query.Goals.Contents.Length == 1 && query.Goals.Contents.Single() is Variable)
        {
            shell.WriteLine("THERE IS AS YET INSUFFICIENT DATA FOR A MEANINGFUL ANSWER.", LogLevel.Cmt);
            shell.No();
            yield return scope;
            yield break;
        }

        var scope_ = scope;

        if (Interactive)
        {
            var any = false;
            await foreach (var s in solutions)
            {
                if (!any)
                {
                    any = true;
                }
                else
                {
                    if (!scope_.TraceEnabled)
                    {
                        shell.WriteLine(" ∨", LogLevel.Rpl);
                    }
                }

                yield return scope;
                if (s.Substitutions.Any())
                {
                    var join = s.Simplify().Substitutions.Join(s => s.Explain());
                    shell.Write($"{join}", LogLevel.Ans);
                    if (scope_.TraceEnabled)
                    {
                        shell.WriteLine();
                    }
                }
                else
                {
                    shell.Yes(nl: false, LogLevel.Rpl);
                }

                switch (char.ToLower(nonInteractiveSolve ? ' ' : shell.ReadChar(true)))
                {
                    case 'c':
                        nonInteractiveSolve = true;
                        break;
                    case ' ':
                        break;
                    default:
                        requestCancel.Cancel();
                        break;
                }

                if (!nonInteractiveSolve)
                    nonInteractiveTrace = false;
            }

            if (!any) shell.No(nl: false, LogLevel.Rpl);
            shell.WriteLine(".");
        }
        else
        {
            var rowsDict = (await solutions.CollectAsync())
                .Select(s => s.Simplify()
                    .Substitutions
                    .ToDictionary(r => r.Lhs.Explain(), r => r.Rhs.Explain()))
                .ToArray();

            var cols = query.Goals
                .Contents
                .SelectMany(t => t.Variables)
                .Where(v => !v.Ignored && rowsDict.Any(x => x.ContainsKey(v.Name)))
                .Select(v => v.Name)
                .Distinct()
                .ToArray();

            var rows = rowsDict
                .Select(d => cols.Select(c => d.TryGetValue(c, out var v) ? v : "_")
                    .ToArray())
                .ToArray();

            if (rowsDict.Length > 0 && rows[0].Length == cols.Length)
            {
                shell.WriteTable(cols, rows, accent);
                shell.Yes();
            }
            else
            {
                shell.No();
            }
        }

        if (requestCancel.IsCancellationRequested && executionAbortedCtrlC)
        {
            shell.WriteLine("Execution terminated by the user.", LogLevel.Err);
        }

        Console.CancelKeyPress -= RequestCancel;
        yield return scope;
        yield break;
        void RequestCancel(object _, ConsoleCancelEventArgs args)
        {
            requestCancel.Cancel();
            executionAbortedCtrlC = true;
            args.Cancel = true;
        }
    }
}
