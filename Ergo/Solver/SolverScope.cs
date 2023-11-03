using Ergo.Interpreter;

namespace Ergo.Solver;
//[DebuggerDisplay("{ Explain() }")]
public readonly record struct SolverScope(
    int Depth,
    Atom Module,
    ImmutableArray<Predicate> Callers,
    Predicate Callee,
    InterpreterScope InterpreterScope,
    bool IsCutRequested,
    InstantiationContext InstantiationContext,
    Tracer Tracer
)
{
    public SolverScope(InterpreterScope interp, Atom module, InstantiationContext ctx, Tracer t)
        : this(0, module, ImmutableArray.Create<Predicate>(), default, interp, false, ctx, t)
    {
    }

    public SolverScope WithModule(Atom module) => this with { Module = module };
    public SolverScope WithDepth(int depth) => this with { Depth = depth };
    public SolverScope WithCaller(Predicate caller) => this with { Callers = Callers.Add(caller) };
    public SolverScope WithoutLastCaller() => this with { Callers = Callers.RemoveAt(Callers.Length - 1) };
    public SolverScope WithCallee(Predicate callee) => this with { Callee = callee };
    public SolverScope WithChoicePoint() => this with { IsCutRequested = false };
    public SolverScope WithCut() => this with { IsCutRequested = true };
    public SolverScope WithInterpreterScope(InterpreterScope scope) => this with { InterpreterScope = scope };

    public void Throw(SolverError error, params object[] args) => InterpreterScope.ExceptionHandler.Throw(new SolverException(error, this, args));
    public void Trace(SolverTraceType type, ITerm term) => Tracer.LogTrace(type, term, this);
    public bool ContainsCaller(Predicate caller) => Callers.Reverse().Any(c => c.IsSameDeclarationAs(caller));

    public string Explain()
    {
        var depth = Depth;
        if (depth == 0) return string.Empty;
        var numCallers = Callers.Length;
        var stackTrace = Callers
            .Select((c, i) => $"[{depth - i}] {c.Head?.Explain(canonical: true)}");
        stackTrace = stackTrace.Append($"[{depth - numCallers}] {Callee.Head.Explain(canonical: true)}");
        return "\t" + string.Join("\r\n\t", stackTrace);
    }
}
