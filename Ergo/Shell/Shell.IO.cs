using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Ergo.Shell
{


    public partial class ErgoShell
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint wCodePageID);


        protected virtual string DefaultLineFormatter(LogLine line)
        {
            var lvl = line.Level.ToString().ToUpper();
            return (line.Level switch
            {
                LogLevel.Rpl => $"{line.Message}"
                , LogLevel.Cmt => $"%%% {line.Message}"
                , _ => $"{lvl} {line.Message}"
            })
            .Replace("\n", $"\n{lvl} ");
        }

        public virtual char ReadChar(bool intercept = false)
        {
            return Console.ReadKey(intercept).KeyChar;
        }

        public virtual string ReadLine(string until = "\r\n")
        {
            var sb = new StringBuilder();
            while ((char)Console.Read() is var c) {
                sb.Append(c);
                if (sb.ToString().EndsWith(until)) {
                    break;
                }
            }
            return sb.ToString()[..^until.Length];
        }

        public virtual string Prompt(string until = "\r\n")
        {
            return ReadLine(until);
        }

        protected virtual void WithColors(LogLevel lvl, Solver.SolverTraceType trc, Action action, ConsoleColor? overrideFg = null, ConsoleColor? overrideBg = null)
        {
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            if(!overrideFg.HasValue || !overrideBg.HasValue)
            {
                switch (lvl)
                {
                    case LogLevel.Wrn:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.BackgroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Err:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.BackgroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Inf:
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.BackgroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Ans:
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.BackgroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Cmt:
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.BackgroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Dbg:
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.BackgroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Trc:
                        switch (trc)
                        {
                            case Solver.SolverTraceType.Call:
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                break;
                            case Solver.SolverTraceType.Exit:
                                Console.ForegroundColor = ConsoleColor.Gray;
                                break;
                            case Solver.SolverTraceType.Retn:
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                break;
                            case Solver.SolverTraceType.Resv:
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                break;
                            case Solver.SolverTraceType.Fail:
                                Console.ForegroundColor = ConsoleColor.Red;
                                break;
                            default:
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                break;
                        }
                        Console.BackgroundColor = ConsoleColor.White;
                        break;
                }
            }
            if (overrideFg is { } fg)
            {
                Console.ForegroundColor = fg;
            }
            if (overrideBg is { } bg)
            {
                Console.BackgroundColor = bg;
            }
            action();
            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }

        public virtual void Write(string str, LogLevel lvl = LogLevel.Rpl, Solver.SolverTraceType trc = Solver.SolverTraceType.Call, ConsoleColor? overrideFg = null, ConsoleColor? overrideBg = null)
        {
            var now = DateTime.Now;
            var lines = str.Replace("\r", "").Split('\n').Select(l => new LogLine(l, lvl, now)).ToArray();

            WithColors(lvl, trc, () => {
                foreach (var line in lines.Take(lines.Length - 1)) {
                    Console.WriteLine(LineFormatter(line));
                }
                Console.Write(LineFormatter(lines.Last())  );
            }, overrideFg, overrideBg);
        }

        public virtual void WriteLine(string str = "", LogLevel lvl = LogLevel.Rpl, Solver.SolverTraceType trc = Solver.SolverTraceType.Call, ConsoleColor? overrideFg = null, ConsoleColor? overrideBg = null)
        {
            Write(str, lvl, trc, overrideFg, overrideBg);
            Console.WriteLine();
        }

        public virtual void WriteList(IReadOnlyCollection<string> list)
        {
            var str = "No.";
            if (list.Count > 1) {
                str = "\t" + String.Join("\n\t, ", list) + "\n\t.";
            }
            else if (list.Count == 1) {
                str = "\t" + list.Single() + ".";
            }
            WriteLine(str);
        }

        public virtual void Yes(bool nl = true, LogLevel lvl = LogLevel.Ans)
        {
            Write("\u001b[1m⊤\u001b[0m", lvl);
            if (nl) WriteLine();
        }

        public virtual void No(bool nl = true, LogLevel lvl = LogLevel.Ans)
        {
            Write("\u001b[1m⊥\u001b[0m", lvl, overrideFg: ConsoleColor.DarkRed);
            if (nl) WriteLine();
        }

        public virtual void WriteTable([NotNull] string[] cols, [NotNull] string[][] rows, ConsoleColor accent = ConsoleColor.Black)
        {
            // Prime candidate for refactoring, but low priority
            var n = (int)Math.Floor(Math.Log10(rows.Length) + 1);
            var allRows = rows.Select((r, i) => r.Prepend((i+1).ToString().PadLeft(n, '0'))).Prepend(cols.Prepend("#")).ToArray();
            var lines = allRows
                .Select(row => row
                    .Select(r => r
                        .Replace("\r", "")
                        .Replace("\t", "  ")
                        .Split('\n'))
                        .ToArray())
                .ToArray();
            var longestCells = Enumerable.Range(0, cols.Length + 1)
                .Select(i => lines.Max(r => r[i].Max(c => c.Length)))
                .ToArray();
            var tallestLine = lines.Max(i => i.Max(j => j.Length));
            var described = lines.Select(l => Describe(l).ToArray()).ToArray();
            var tableWidth = described.Max(d => d.Max(s => s.Length)) - 3;

            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            AlternateColors(-1);
            DrawBorder(l: '╔', h: '═', s: '╦', r: '╗');
            for (int i = 0; i < described.Length; i++) {
                for (int j = 0; j < described[i].Length; j++) {
                    AlternateColors(-1);
                    Write("\t║");
                    if(i == 0)
                    {
                        AlternateColors(i);
                    }
                    if(i > 0 && i < described.Length - 1 && j == described[i].Length - 1)
                    {
                        Write("\u001b[4m " + described[i][j] + " \u001b[0m");
                    }
                    else
                    {
                        Write(" " + described[i][j] + " ");
                    }
                    AlternateColors(-1);
                    WriteLine("║ ");
                }
            }
            AlternateColors(-1);
            DrawBorder(l: '╚', h: '═', s: '╩', r: '╝');
            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;

            void AlternateColors(int i)
            {
                if(i % 2 != 0) {
                    Console.ForegroundColor = accent;
                    Console.BackgroundColor = ConsoleColor.White;
                }
                else {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = accent;
                }
            }

            void DrawBorder(char l, char h, char s, char r)
            {
                var str = new string(h, longestCells[0] + 2);
                for (int i = 1; i < longestCells.Length; i++) {
                    str += s + new string(h, longestCells[i] + 2);
                        
                }
                WriteLine($"\t{l}{str}{r}");
            }

            IEnumerable<string> Describe(string[][] row)
            {
                for (int j = 0; j < tallestLine; j++) {
                    if (row.All(r => r.Length <= j))
                        continue;
                    var str = "";
                    for (int i = 0; i < row.Length; i++) {
                        var cell = row[i].Length > j ? row[i][j] : String.Empty;
                        str += cell.PadRight(longestCells[i], ' ');
                        if (i < row.Length - 1) {
                            str += " ║ ";
                        }
                    }
                    yield return str;
                }
            }
        }
    }
}
