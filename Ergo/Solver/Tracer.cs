using System.ComponentModel;
using System.Diagnostics;

namespace Ergo.Solver;

public class Tracer
{
    private readonly Stopwatch _sw = new();

    public Tracer() { }
    public event Action<Tracer, SolverScope, SolverTraceType, string> Trace;
    protected virtual string FormatTrace(SolverTraceType type, string content, SolverScope scope)
    {
        var desc = type.GetAttribute<DescriptionAttribute>().Description;
        return $"{desc}: ({scope.Depth:00}) [{_sw.Elapsed.TotalMilliseconds:0.0000}ms] {content}"
            .PadRight(64);
    }
    public void LogTrace(SolverTraceType type, ITerm term, SolverScope scope) => LogTrace(type, () => term.Explain(), scope);
    private void LogTrace(SolverTraceType type, Func<string> s, SolverScope scope)
    {
        if (Trace is null || Trace.GetInvocationList().Length == 0)
            return;
        Trace?.Invoke(this, scope, type, FormatTrace(type, s(), scope));
        _sw.Restart();
    }
}
