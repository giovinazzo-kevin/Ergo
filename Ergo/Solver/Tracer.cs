using System.ComponentModel;
using System.Diagnostics;

namespace Ergo.Solver;

public class Tracer
{
    private static readonly Dictionary<SolverTraceType, string> DescMap = new();
    private readonly Dictionary<Signature, Stack<TimeSpan>> TimeStacks = new();
    private readonly Stopwatch _sw = new();

    static Tracer()
    {
        foreach (var val in Enum.GetValues<SolverTraceType>())
        {
            DescMap[val] = val.GetAttribute<DescriptionAttribute>().Description;
        }
    }



    public Tracer() { _sw.Start(); }
    public event Action<Tracer, SolverScope, SolverTraceType, string> Trace;
    protected virtual string FormatTrace(SolverTraceType type, Maybe<TimeSpan> duration, string content, SolverScope scope)
    {
        if (duration.TryGetValue(out var dur))
            return $"({scope.Depth:00}) {DescMap[type]} [{dur.TotalMilliseconds:000.00}ms]: {content}";
        return $"({scope.Depth:00}) {DescMap[type]}: {content}";
    }
    public void LogTrace(SolverTraceType type, ITerm term, SolverScope scope)
    {
        if (Trace is null || Trace.GetInvocationList().Length == 0)
            return;
        var sig = term.GetSignature();
        if (!TimeStacks.TryGetValue(sig, out var stack))
            stack = TimeStacks[sig] = new Stack<TimeSpan>();
        var duration = default(Maybe<TimeSpan>);
        if (type == SolverTraceType.Call)
        {
            stack.Push(_sw.Elapsed);
        }
        else if (type == SolverTraceType.Backtrack && stack.Count > 0)
        {
            duration = _sw.Elapsed - stack.Pop();
        }
        Trace?.Invoke(this, scope, type, FormatTrace(type, duration, term.Explain(), scope));
    }
}
