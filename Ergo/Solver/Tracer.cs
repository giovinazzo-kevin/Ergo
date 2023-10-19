using System.ComponentModel;
using System.Diagnostics;

namespace Ergo.Solver;

public class Tracer
{
    private static readonly Dictionary<SolverTraceType, string> DescMap = new();

    static Tracer()
    {
        foreach (var val in Enum.GetValues<SolverTraceType>())
        {
            DescMap[val] = val.GetAttribute<DescriptionAttribute>().Description;
        }
    }


    private readonly Stopwatch _sw = new();

    public Tracer() { }
    public event Action<Tracer, SolverScope, SolverTraceType, string> Trace;
    protected virtual string FormatTrace(SolverTraceType type, string content, SolverScope scope)
    {
        return $"{DescMap[type]}: ({scope.Depth:00}) {content}";
    }
    public void LogTrace(SolverTraceType type, ITerm term, SolverScope scope)
    {
        if (Trace is null || Trace.GetInvocationList().Length == 0)
            return;
        LogTrace(type, () => term.GetSignature().Explain(), scope);
    }
    private void LogTrace(SolverTraceType type, Func<string> s, SolverScope scope)
    {
        Trace?.Invoke(this, scope, type, FormatTrace(type, s(), scope));
        _sw.Restart();
    }
}
