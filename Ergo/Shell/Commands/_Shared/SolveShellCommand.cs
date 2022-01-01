using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Solver;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
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

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
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
            var solver = shell.CreateSolver(ref scope);
            var parsed = shell.Parse<Query>(scope, userQuery).Value;
            if (!parsed.HasValue)
            {
                return;
            }
            var query = parsed.GetOrDefault();
            shell.WriteLine(query.Goals.Explain(), LogLevel.Dbg);
            if (scope.TraceEnabled)
            {
                solver.Trace += (type, trace) => shell.WriteLine(trace, LogLevel.Trc, type);
            }
            var solutions = solver.Solve(query); // Solution graph is walked lazily
            if (query.Goals.Contents.Length == 1 && query.Goals.Contents.Single() is Variable)
            {
                // SWI-Prolog goes with The Ultimate Question, we'll go with The Last Question instead.
                shell.WriteLine("THERE IS AS YET INSUFFICIENT DATA FOR A MEANINGFUL ANSWER.", LogLevel.Cmt);
                shell.No();
                return;
            }
            var scope_ = scope;
            scope.ExceptionHandler.Try(scope, () => {
                if (Interactive)
                {
                    shell.WriteLine("Press space to yield more solutions:", LogLevel.Inf);
                    var any = false;
                    foreach (var s in solutions)
                    {
                        if (any)
                        {
                            if (shell.ReadChar(true) != ' ')
                            {
                                break;
                            }
                            if (!scope_.TraceEnabled)
                            {
                                shell.WriteLine(" ∨", LogLevel.Rpl);
                                shell.Write(String.Empty, LogLevel.Ans);
                            }
                        }
                        else
                        {
                            any = true;
                            shell.Write(String.Empty, LogLevel.Ans);
                        }
                        if (s.Substitutions.Any())
                        {
                            var join = String.Join(", ", s.Simplify().Select(s => s.Explain()));
                            if (scope_.TraceEnabled)
                            {
                                shell.Write(String.Empty, LogLevel.Ans);
                            }
                            shell.Write($"{join}", LogLevel.Rpl);
                            if(scope_.TraceEnabled)
                            {
                                shell.WriteLine();
                            }
                        }
                        else
                        {
                            shell.Yes(nl: false, LogLevel.Rpl);
                        }
                    }
                    if (!any) shell.No(nl: true, LogLevel.Rpl);
                    shell.WriteLine();
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
