using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{

    public abstract class SolveShellCommand : ShellCommand
    {
        public readonly bool Interactive;
        public readonly ConsoleColor DefaultAccentColor;

        static string GetColorAlternatives()
        {
            var colorMap = Enum.GetNames(typeof(ConsoleColor))
                .Select(v => v.ToString())
                .ToList();
            return String.Join('|', colorMap);
        }

        protected SolveShellCommand(string[] names, string desc, int priority, bool interactive, ConsoleColor accentColor) 
            : base(names, desc, $@"(?<color>{GetColorAlternatives()})?\s*(?<query>(?:[^\s].*\s*=)?\s*[^\s].*)", priority)
        {
            Interactive = interactive;
            DefaultAccentColor = accentColor;
        }

        public override void Callback(Shell shell, Match m)
        {
            var userQuery = m.Groups["query"].Value;
            var accent = m.Groups["color"] is { Success: true, Value: var v } 
                ? Enum.Parse<ConsoleColor>(v, true)
                : DefaultAccentColor;
            if (!userQuery.EndsWith('.'))
            {
                // Syntactic sugar
                userQuery += '.';
            }
            var parsed = shell.Parse<Query>(userQuery).Value;
            if (!parsed.HasValue)
            {
                return;
            }
            var query = parsed.Reduce(some => some, () => default);
            shell.WriteLine(query.Goals.Explain(), LogLevel.Dbg);

            var solutions = shell.Interpreter.Solve(query, Maybe.Some(shell.CurrentModule)); // Solution graph is walked lazily
            if (query.Goals.Contents.Length == 1 && query.Goals.Contents.Single() is Variable)
            {
                // SWI-Prolog goes with The Ultimate Question, we'll go with The Last Question instead.
                shell.WriteLine("THERE IS AS YET INSUFFICIENT DATA FOR A MEANINGFUL ANSWER.", LogLevel.Cmt);
                shell.No();
                return;
            }

            shell.ExceptionHandler.Try(() => {
                if (Interactive)
                {
                    shell.WriteLine("Press space to yield more solutions:", LogLevel.Inf);
                    var any = false;
                    foreach (var s in solutions)
                    {
                        any = true;
                        if (s.Substitutions.Any())
                        {
                            var join = String.Join(", ", s.Simplify().Select(s => s.Explain()));
                            shell.WriteLine($"\t| {join}");
                            if (shell.ReadChar(true) != ' ')
                            {
                                break;
                            }
                        }
                    }
                    if (any) shell.Yes(); else shell.No();
                }
                else
                {
                    var cols = query.Goals
                        .Contents
                        .SelectMany(t => t.Variables)
                        .Where(v => !v.Ignored)
                        .Select(v => v.Name)
                        .Distinct()
                        .ToArray();
                    var rows = solutions
                        .Select(s => s.Simplify()
                            .Select(r => r.Rhs.Explain())
                            .ToArray())
                        .ToArray();
                    if (rows.Length > 0 && rows[0].Length == cols.Length)
                    {
                        shell.WriteTable(cols, rows, accent);
                        shell.Yes();
                    }
                    else
                    {
                        shell.No();
                    }
                }
            });
        }
    }
}
