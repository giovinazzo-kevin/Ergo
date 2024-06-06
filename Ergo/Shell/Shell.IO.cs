using Ergo.Runtime;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Ergo.Shell;

internal class TreeNode<T>(T val, params TreeNode<T>[] children)
{
    public readonly T Value = val;
    public Maybe<TreeNode<T>> Parent { get; set; }
    public TreeNode<T>[] Children { get; set; } = children;
}

public partial class ErgoShell
{

    const int STD_INPUT_HANDLE = -10;
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCP(uint wCodePageID);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

    protected virtual string DefaultLineFormatter(LogLine line)
    {
        var lvl = line.Level.ToString().ToUpper();
        return (line.Level switch
        {
            LogLevel.Rpl => $"{line.Message}"
            ,
            LogLevel.Cmt => $"%%% {line.Message}"
            ,
            _ => $"{lvl} {line.Message}"
        })
        .Replace("\n", $"\n{lvl} ");
    }

    public virtual char ReadChar(bool intercept = false) => InputReader.ReadChar(intercept);

    public virtual string ReadLine(string until = "\r\n")
    {
        var sb = new StringBuilder();
        WithColors(() =>
        {
            try
            {
                while ((char)Console.Read() is var c)
                {
                    sb.Append(c);
                    if (sb.ToString().EndsWith(until))
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, (ConsoleColor.Black, ConsoleColor.White));
        if (sb.Length >= until.Length)
            return sb.ToString()[..^until.Length];
        return string.Empty;
    }

    public virtual string Prompt(string until = "\r\n") => ReadLine(until);

    protected virtual (ConsoleColor Foreground, ConsoleColor Background) GetColors(LogLevel lvl, TraceType trc = default)
    {
        return lvl switch
        {
            LogLevel.Wrn => (ConsoleColor.DarkYellow, ConsoleColor.White),
            LogLevel.Inf => (ConsoleColor.DarkCyan, ConsoleColor.White),
            LogLevel.Ans => (ConsoleColor.DarkBlue, ConsoleColor.White),
            LogLevel.Cmt => (ConsoleColor.DarkGreen, ConsoleColor.White),
            LogLevel.Dbg => (ConsoleColor.DarkGray, ConsoleColor.White),
            LogLevel.Err => (ConsoleColor.Red, ConsoleColor.White),
            LogLevel.Trc => trc switch
            {
                TraceType.Call => (ConsoleColor.Black, ConsoleColor.White),
                TraceType.Exit => (ConsoleColor.DarkGray, ConsoleColor.White),
                TraceType.Expansion => (ConsoleColor.DarkGreen, ConsoleColor.White),
                TraceType.BuiltInResolution => (ConsoleColor.DarkYellow, ConsoleColor.White),
                TraceType.Backtrack => (ConsoleColor.Gray, ConsoleColor.White),
                TraceType.TailCallOptimization => (ConsoleColor.DarkCyan, ConsoleColor.White),
                _ => (ConsoleColor.Black, ConsoleColor.White),
            },
            _ => (Console.ForegroundColor, Console.BackgroundColor),
        };
    }

    protected virtual void WithColors(Action action, (ConsoleColor Foreground, ConsoleColor Background) colors)
    {
        if (!UseColors)
        {
            action();
            return;
        }
        var oldFg = Console.ForegroundColor;
        var oldBg = Console.BackgroundColor;
        (Console.ForegroundColor, Console.BackgroundColor) = colors;
        action();
        Console.ForegroundColor = oldFg;
        Console.BackgroundColor = oldBg;
    }

    public virtual void Write(string str, LogLevel lvl = LogLevel.Rpl, TraceType trc = TraceType.Call, ConsoleColor? overrideFg = null, ConsoleColor? overrideBg = null)
    {
        var now = DateTime.Now;
        var lines = str.Replace("\r", "").Split('\n').Select(l => new LogLine(l, lvl, now)).ToArray();
        var colors = GetColors(lvl, trc);
        if (overrideFg.HasValue) colors.Foreground = overrideFg.Value;
        if (overrideBg.HasValue) colors.Background = overrideBg.Value;
        WithColors(() =>
        {
            foreach (var line in lines)
                EncodeAndWrite(line);
        }, colors);

        void EncodeAndWrite(LogLine line)
        {
            var str = LineFormatter(line);
            if (!UseUnicodeSymbols)
            {
                str = str.Replace("⊤", "true");
                str = str.Replace("⊥", "false");
            }
            var bytes = Out.Encoding.GetBytes(str);
            var encodedString = Out.Encoding.GetString(bytes);
            Out.Write(encodedString);
            Out.Flush();
        }
    }

    public virtual void WriteLine(string str = "", LogLevel lvl = LogLevel.Rpl, TraceType trc = TraceType.Call, ConsoleColor? overrideFg = null, ConsoleColor? overrideBg = null)
    {
        var lines = str.Split(Environment.NewLine);
        foreach (var line in lines)
        {
            Write(line, lvl, trc, overrideFg, overrideBg);
            Out.WriteLine();
            Out.Flush();
        }
    }

    public virtual void Yes(bool nl = true, LogLevel lvl = LogLevel.Ans)
    {
        if (UseANSIEscapeSequences)
        {
            Write($"\u001b[1m⊤\u001b[0m", lvl);
        }
        else
        {
            Write("⊤", lvl);
        }
        if (nl) WriteLine();
    }

    public virtual void No(bool nl = true, LogLevel lvl = LogLevel.Ans)
    {
        if (UseANSIEscapeSequences)
        {
            Write($"\u001b[1m⊥\u001b[0m", lvl, overrideFg: ConsoleColor.DarkRed);
        }
        else
        {
            Write("⊥", lvl, overrideFg: ConsoleColor.DarkRed);
        }
        if (nl) WriteLine();
    }

    public virtual void WriteTree<T, K>(
        T value,
        Func<T, K> getKey,
        Func<T, IEnumerable<T>> getChildren,
        Func<T, string> toString,
        Func<T, bool> shouldPrintCyclicTerm,
        ConsoleColor? overrideFg = null)
    {
        var seen = new HashSet<K>();
        var tree = BuildTree(value);
        WriteLine(toString(value));
        for (var i = 0; i < tree.Children.Length; i++)
        {
            WriteNode(tree, tree.Children[i], 0);
        }

        void WriteNode(TreeNode<T> parent, TreeNode<T> node, int indent = 0)
        {
            var last = parent.Children.Last() == node;
            var key = getKey(node.Value);
            var cycle = seen.Contains(key);
            if (cycle && !shouldPrintCyclicTerm(node.Value))
                return;
            var spaces = Enumerable.Range(0, indent).Join(_ => "   ", "");
            //if (indent > 0) spaces = $"{spaces}│    ";
            var connector = new string('─', 2);
            Write(spaces);
            var ancestors = GetAncestors(parent).ToArray();
            for (var j = 0; j < ancestors.Length; j++)
            {
                Write("\b\b\b");
            }

            for (var j = ancestors.Length - 1; j >= 1; j--)
            {
                if (ancestors[j].Children.Last() != ancestors[j - 1])
                {
                    Write("│  ", overrideFg: ConsoleColor.Gray);
                }
                else
                {
                    Write("   ");
                }
            }

            Write(last ? "└" : "├", overrideFg: ConsoleColor.Gray);
            Write(connector, overrideFg: ConsoleColor.Gray);
            if (cycle)
            {
                Write(toString(node.Value), overrideFg: ConsoleColor.DarkGray);
                WriteLine("…", overrideFg: ConsoleColor.DarkGray);
                return;
            }
            else
            {
                Write(toString(node.Value), overrideFg: overrideFg ?? ConsoleColor.Black);
            }

            WriteLine();
            seen.Add(key);
            for (var i = 0; i < node.Children.Length; i++)
            {
                WriteNode(node, node.Children[i], indent + 1);
            }
        }

        IEnumerable<TreeNode<T>> GetAncestors(TreeNode<T> node)
        {
            var cur = node;
            while (true)
            {
                yield return cur;
                cur = cur.Parent.GetOr(default);
                if (cur is null) yield break;
            }
        }

        bool IsPathCyclic(TreeNode<T> parent, TreeNode<T> current, out ImmutableArray<TreeNode<T>> cycle, ImmutableArray<TreeNode<T>> seen)
        {
            cycle = [];
            if (parent == current || seen.Contains(current))
            {
                cycle = ImmutableArray.CreateRange(seen.Skip(seen.LastIndexOf(current)));
                return true;
            }

            seen = seen.Add(current);
            foreach (var child in current.Children)
            {
                if (IsPathCyclic(current, child, out cycle, seen))
                    return true;
            }

            return false;
        }

        TreeNode<T> BuildTree(T value, Dictionary<K, TreeNode<T>> dict = null)
        {
            dict ??= [];
            var key = getKey(value);
            if (dict.TryGetValue(key, out var node))
                return new(value, node.Children);
            var ret = dict[key] = new(value);
            var children = getChildren(value)
                .Select(child => BuildTree(child, dict))
                .ToArray();
            foreach (var c in children)
            {
                c.Parent = ret;
            }

            ret.Children = children;
            return ret;
        }

    }

    public virtual void WriteTable([NotNull] string[] cols, [NotNull] string[][] rows, ConsoleColor accent = ConsoleColor.Black)
    {
        // Prime candidate for refactoring, but low priority
        var n = (int)Math.Floor(Math.Log10(rows.Length) + 1);
        var allRows = rows.Select((r, i) => r.Prepend((i + 1).ToString().PadLeft(n, '0'))).Prepend(cols.Prepend("#")).ToArray();
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
        for (var i = 0; i < described.Length; i++)
        {
            for (var j = 0; j < described[i].Length; j++)
            {
                AlternateColors(-1);
                Write("\t║");
                if (i == 0)
                {
                    AlternateColors(i);
                }

                if (i > 0 && i < described.Length - 1 && j == described[i].Length - 1)
                {
                    if (UseANSIEscapeSequences)
                    {
                        Write("\u001b[4m " + described[i][j] + " \u001b[0m");
                    }
                    else
                    {
                        Write(" " + described[i][j] + " ");
                    }
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
            if (!UseColors)
                return;

            if (i % 2 != 0)
            {
                Console.ForegroundColor = accent;
                Console.BackgroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = accent;
            }
        }

        void DrawBorder(char l, char h, char s, char r)
        {
            var str = new string(h, longestCells[0] + 2);
            for (var i = 1; i < longestCells.Length; i++)
            {
                str += s + new string(h, longestCells[i] + 2);

            }

            WriteLine($"\t{l}{str}{r}");
        }

        IEnumerable<string> Describe(string[][] row)
        {
            for (var j = 0; j < tallestLine; j++)
            {
                if (row.All(r => r.Length <= j))
                    continue;
                var str = "";
                for (var i = 0; i < row.Length; i++)
                {
                    var cell = row[i].Length > j ? row[i][j] : string.Empty;
                    str += cell.PadRight(longestCells[i], ' ');
                    if (i < row.Length - 1)
                    {
                        str += " ║ ";
                    }
                }

                yield return str;
            }
        }
    }
}
