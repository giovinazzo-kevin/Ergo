using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public abstract class SolveShellCommand : ShellCommand
{
    public readonly ConsoleColor DefaultAccentColor;
    public readonly SolveMode Mode;

    private readonly Dictionary<int, CachedVM> VMCache = new();

    record class CachedVM(ErgoVM VM, CompilerFlags Flags);

    public enum SolveMode
    {
        Interactive,
        Tabled,
        Benchmark
    }

    static string GetColorAlternatives()
    {
        var colorMap = Enum.GetNames(typeof(ConsoleColor))
            .Select(v => v.ToString())
            .ToList();
        return colorMap.Join("|");
    }

    protected SolveShellCommand(string[] names, string desc, int priority, SolveMode mode, ConsoleColor accentColor)
        : base(names, desc, $@"(?<color>{GetColorAlternatives()})?\s*(?<query>(?:[^\s].*\s*=)?\s*[^\s].*)", true, priority, caseInsensitive: true)
    {
        Mode = mode;
        DefaultAccentColor = accentColor;
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        await Task.CompletedTask;
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

        var key = scope.KnowledgeBase.GetHashCode() + scope.KnowledgeBase.Count;
        if (!VMCache.TryGetValue(key, out var cached) || cached.Flags != scope.CompilerFlags)
        {
            //foreach (var (k, s) in VMCache)
            //    s.Dispose();
            VMCache.Clear();
            cached = VMCache[key] = new(shell.Facade.BuildVM(scope.KnowledgeBase), scope.CompilerFlags);
        }
        var parsed = scope.InterpreterScope.Parse<Query>(userQuery);
        if (!parsed.TryGetValue(out var query))
        {
            yield return scope;
            yield break;
        }

        shell.WriteLine(query.Goals.Explain(false), LogLevel.Dbg);
        var (nonInteractiveTrace, nonInteractiveSolve) = (false, false);
        if (scope.TraceEnabled)
        {
            //solverScope.Tracer.Trace += (_, __, type, trace) =>
            //{
            //    shell.Write(trace, LogLevel.Trc, type);
            //    shell.Write("? ", LogLevel.Rpl, overrideFg: ConsoleColor.DarkMagenta);
            //    switch (char.ToLower(nonInteractiveTrace ? ' ' : shell.ReadChar(true)))
            //    {
            //        case 'c':
            //            shell.Write("contn", LogLevel.Rpl, overrideFg: ConsoleColor.Magenta);
            //            nonInteractiveTrace = true;
            //            break;
            //        case ' ':
            //            shell.Write("creep", LogLevel.Rpl, overrideFg: ConsoleColor.DarkMagenta);
            //            break;
            //        default:
            //            shell.Write("abort", LogLevel.Rpl, overrideFg: ConsoleColor.Red);
            //            requestCancel.Cancel();
            //            break;
            //    }

            //    shell.WriteLine();
            //};
        }
        cached.VM.Query = cached.VM.CompileQuery(query, scope.CompilerFlags);
        if (query.Goals.Contents.Length == 1 && query.Goals.Contents.Single() is Variable)
        {
            shell.WriteLine("THERE IS AS YET INSUFFICIENT DATA FOR A MEANINGFUL ANSWER.", LogLevel.Cmt);
            shell.No();
            yield return scope;
            yield break;
        }

        var scope_ = scope;

        if (Mode == SolveMode.Interactive)
        {
            var any = false;
            var solutions = cached.VM.RunInteractive(); // Solution graph is walked lazily
            foreach (var s in solutions)
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
        else if (Mode == SolveMode.Tabled)
        {
            cached.VM.Run();
            var rowsDict = cached.VM.Solutions
                .Select(s => s
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
        else if (Mode == SolveMode.Benchmark)
        {
            var sw = new Stopwatch();
            sw.Start();
            cached.VM.Run();
            sw.Stop();
            if (cached.VM.State == ErgoVM.VMState.Fail)
                shell.No();
            else shell.Yes();
            shell.WriteLine($"{cached.VM.NumSolutions} solution{(cached.VM.NumSolutions == 1 ? "" : "s")} ({(sw.Elapsed.TotalMilliseconds):0.000}ms).", LogLevel.Cmt);
        }

        if (requestCancel.IsCancellationRequested && executionAbortedCtrlC)
        {
            shell.WriteLine("Execution terminated by the user.", LogLevel.Err);
        }

        cached.VM.Memory.Clear();
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
