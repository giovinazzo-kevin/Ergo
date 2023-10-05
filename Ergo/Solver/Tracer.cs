using System.ComponentModel;

namespace Ergo.Solver;

public class Tracer
{
    public readonly ErgoSolver Solver;
    public Tracer(ErgoSolver solver) { Solver = solver; }
    public event Action<Tracer, SolverScope, SolverTraceType, string> Trace;
    protected virtual string FormatTrace(SolverTraceType type, string content, SolverScope scope)
    {
        return $"{type.GetAttribute<DescriptionAttribute>().Description}: ({scope.Depth:00}) {content}"
            .PadRight(64);
    }
    public void LogTrace(SolverTraceType type, ITerm term, SolverScope scope) => LogTrace(type, () => term.Explain(), scope);
    private void LogTrace(SolverTraceType type, Func<string> s, SolverScope scope)
    {
        if (Trace is null || Trace.GetInvocationList().Length == 0)
            return;
        Trace?.Invoke(this, scope, type, FormatTrace(type, s(), scope));
    }
}
